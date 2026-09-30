namespace Battle.Internal.Npc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Ai.World.Recovery;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Source;
    using Core.Services;
    using Godot;
    using GameServiceProvider = Services.GameServiceProvider;

    /// <summary>
    /// A faction's home on the map (forest/camp/cemetery/city): keeps its population at
    /// <see cref="_maxCount"/>, spawning from its Npc.json id list at free spots in the radius.
    /// A burned body (final death) or a body risen into another faction frees the slot and
    /// schedules a replacement; the global cap is enforced by <see cref="INpcPopulationService"/>.
    /// Population state is capturable (see <see cref="IPersistentSpawnPoint"/>): respawns are due
    /// at absolute GAME minutes, so a restored state resumes the timers instead of refilling the
    /// camp. The sandbox has no save system — here the point always fills itself on ready.
    /// </summary>
    [GlobalClass]
    internal partial class NpcSpawnPoint : Node2D, IPersistentSpawnPoint
    {
        [Export] private string[] _npcIds = [];
        [Export] private int _maxCount = 3;
        [Export] private float _spawnRadius = 200f;
        [Export] private bool _spawnAsGroup;
        [Export] private bool _spawnOnReady = true;

        /// <summary>Stable save identity; empty = the scene-tree path (fine until the node is renamed/moved).</summary>
        [Export] private string _pointId = "";

        // Replacement delays in GAME minutes (at the default clock speed one real second is one
        // game minute); the range spreads a wiped camp's comeback over time.
        [Export] private float _respawnDelayMinMinutes = 10f;
        [Export] private float _respawnDelayMaxMinutes = 30f;

        // Day-phase pressure on the respawn pace: the rolled delay is scaled by the multiplier
        // of the phase active WHEN THE SLOT FREES (an undead camp: night < 1 spawns eagerly,
        // day > 1 barely). Deliberately not re-evaluated at the due moment — simplicity first.
        [Export] private float _nightDelayMultiplier = 1f;
        [Export] private float _morningDelayMultiplier = 1f;
        [Export] private float _dayDelayMultiplier = 1f;
        [Export] private float _eveningDelayMultiplier = 1f;

        // Home doubles as a rest spot: wounded residents come back and heal inside (the Recovery
        // activity gate), and so does any non-fighting guest the owners don't consider an enemy —
        // a friendly player rests in a friendly camp, a hostile one gets nothing (tracker #146).
        [Export] private bool _recoveryZone = true;

        /// <summary>The zone must cover the whole spawn spread: NPC homes are rolled within _spawnRadius.</summary>
        private const float RecoveryZoneMargin = 100f;

        private readonly HashSet<string> _ownedInstanceIds = [];
        private readonly List<double> _pendingDueMinutes = [];
        private readonly RandomNumberGenerator _rnd = new();
        private readonly IGameServiceProvider _gameServiceProvider = GameServiceProvider.Instance;
        private INpcProvider? _provider;
        private IGameEventBus? _gameEventBus;
        private INpcPopulationService? _population;
        private ISpawnPointRegistry? _spawnRegistry;
        private IWorldClock? _worldClock;
        private IRestRecoveryService? _recovery;
        private IFactionRelationService? _factionRelations;
        private EntityGroup? _group;
        private double _fallbackMinutes;

        /// <summary>Faction of the first known id — a point houses one faction; null until the data loads.</summary>
        private Fractions? Fraction => field ??= ResolveFraction();

        public string PointId => string.IsNullOrEmpty(_pointId) ? GetPath().ToString() : _pointId;

        /// <summary>Now in absolute game minutes; without a clock (sandbox scenes) approximates
        /// the default speed of one game minute per real second.</summary>
        private double NowMinutes => _worldClock != null ? _worldClock.Day * 1440 + _worldClock.MinuteOfDay : _fallbackMinutes;

        public override void _Ready()
        {
            _rnd.Randomize();
            _provider = _gameServiceProvider.GetService<INpcProvider>();
            _gameEventBus = _gameServiceProvider.GetService<IGameEventBus>();
            _population = _gameServiceProvider.GetService<INpcPopulationService>();
            _spawnRegistry = _gameServiceProvider.GetService<ISpawnPointRegistry>();
            _worldClock = _gameServiceProvider.GetService<IWorldClock>();
            _recovery = _gameServiceProvider.GetService<IRestRecoveryService>();
            _factionRelations = _gameServiceProvider.GetService<IFactionRelationService>();
            if (_recoveryZone) _recovery?.RegisterZone(this, () => GlobalPosition, _spawnRadius + RecoveryZoneMargin, CanRestHere);
            _spawnRegistry?.Register(this);
            _gameEventBus?.Subscribe<NpcFinalDeathEvent>(OnFinalDeath);
            _gameEventBus?.Subscribe<NpcFactionChangedEvent>(OnFactionChanged);

            // Deferred so the providers finish loading their JSON before the first spawn.
            // The sandbox never restores a saved world, so nothing competes for the initial fill.
            if (_spawnOnReady) CallDeferred(nameof(FillToCapacity));
        }

        public override void _ExitTree()
        {
            _recovery?.UnregisterZone(this);
            _spawnRegistry?.Unregister(this);
            _gameEventBus?.Unsubscribe<NpcFinalDeathEvent>(OnFinalDeath);
            _gameEventBus?.Unsubscribe<NpcFactionChangedEvent>(OnFactionChanged);
        }

        // TODO:
        // Часы стреляют два эвента (HoursPasses, MinutesPassed), вместо логики в процессе перевести на эти эвенты
        public override void _Process(double delta)
        {
            if (_worldClock == null) _fallbackMinutes += delta;
            if (_pendingDueMinutes.Count == 0) return;

            double now = NowMinutes;
            for (int i = _pendingDueMinutes.Count - 1; i >= 0; i--)
            {
                if (_pendingDueMinutes[i] > now) continue;
                if (_ownedInstanceIds.Count >= _maxCount)
                {
                    _pendingDueMinutes.RemoveAt(i); // obsolete debt: the roster is full again
                    continue;
                }

                if (TrySpawnOne()) _pendingDueMinutes.RemoveAt(i);
                else _pendingDueMinutes[i] = now + 1; // population cap busy: retry in a game minute
            }
        }

        public SpawnPointSaveData CaptureState() => new() { Id = PointId, Alive = _ownedInstanceIds.Count, PendingDueMinutes = [.. _pendingDueMinutes], };

        /// <summary>Restore path: exactly the captured alive count returns (identities re-roll)
        /// and the respawn timers resume in game time.</summary>
        public void RestoreState(SpawnPointSaveData data)
        {
            for (int i = 0; i < data.Alive && _ownedInstanceIds.Count < _maxCount; i++)
                if (!TrySpawnOne())
                    break; // population cap: the world is fuller than it was at save time

            _pendingDueMinutes.Clear();
            _pendingDueMinutes.AddRange(data.PendingDueMinutes);
        }

        public void FillFresh() => FillToCapacity();

        private void FillToCapacity()
        {
            while (_ownedInstanceIds.Count < _maxCount && TrySpawnOne())
            {
            }
        }

        // TODO:
        // Смена алгоритма спавна: Вместо спавна сразу же по мере необходимости (один нпс на точке погиб => сразу же спавн нового) меням на следующиЙ:
        // 1 - После первого спавна точка ожидает до момента когда все ее нпс погибнут
        // 2 - После смерти последнего нпс для данной точки начинается таймер перезарядки
        // 3 - По истечении таймера точка выбирает случайное число между 1 и максимальным кол-во нпс
        // 4 - Спавн по обычному сценарию выбранного кол-ва нпс
        // 5 - При кол-ве нпс > 1 все новые созданные нпс объединяются в одну группу (если установлен флаг групп) до максимума.
        // Если остаток > 1 создается новая группа и последующие нпс добавляются уже в нее
        private bool TrySpawnOne()
        {
            if (_npcIds.Length == 0 || _ownedInstanceIds.Count >= _maxCount) return false;
            if (GetParent() is not Node2D world) return false;
            if (_population is { } population && !population.TryReserve()) return false;

            try
            {
                string npcId = _npcIds[_rnd.RandiRange(0, _npcIds.Length - 1)];
                var definition = _provider!.CreateDefinition(npcId);

                var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
                npc.InjectServices(_gameServiceProvider);
                // Position BEFORE AddChild: a body that enters the physics space at (0,0) and is
                // teleported a statement later drags any body overlapping the origin (the player
                // spawns there) along with it through MoveAndSlide's platform inheritance.
                npc.Position = world.ToLocal(RollSpotInRadius());
                world.AddChild(npc); // _Ready builds the components ApplyDefinition configures
                npc.ApplyDefinition(definition, _gameServiceProvider);
                AddToGroupIfNeeded(npc);
                _ownedInstanceIds.Add(npc.InstanceId);
                return true;
            }
            catch (Exception e)
            {
                GD.PrintErr($"NpcSpawnPoint: spawn failed: {e.Message}");
                return false;
            }
        }

        private Vector2 RollSpotInRadius()
        {
            var offset = new Vector2(_rnd.RandfRange(-_spawnRadius, _spawnRadius), _rnd.RandfRange(-_spawnRadius, _spawnRadius));
            return GlobalPosition + offset;
        }

        private void AddToGroupIfNeeded(BaseNpc npc)
        {
            if (!_spawnAsGroup) return;
            _group ??= new EntityGroup(maxMembers: _maxCount);
            _group.TryAddToGroup(npc);
        }

        /// <summary>A burned body of ours: the slot frees up, a replacement is scheduled.</summary>
        private void OnFinalDeath(NpcFinalDeathEvent evnt) => ReleaseSlot(evnt.InstanceId);

        /// <summary>A body of ours rose into another faction: the rising is wild, we refill the original.</summary>
        private void OnFactionChanged(NpcFactionChangedEvent evnt) => ReleaseSlot(evnt.InstanceId);

        private void ReleaseSlot(string instanceId)
        {
            if (!_ownedInstanceIds.Remove(instanceId)) return;
            float delay = _rnd.RandfRange(_respawnDelayMinMinutes, _respawnDelayMaxMinutes) * PhaseDelayMultiplier;
            _pendingDueMinutes.Add(NowMinutes + delay);
        }

        private float PhaseDelayMultiplier => _worldClock?.Phase switch
        {
            DayPhase.Night => _nightDelayMultiplier,
            DayPhase.Morning => _morningDelayMultiplier,
            DayPhase.Evening => _eveningDelayMultiplier,
            DayPhase.Day => _dayDelayMultiplier,
            _ => 1f, // no clock (sandbox): the plain roll
        };

        private Fractions? ResolveFraction()
        {
            if (_provider == null) return null;

            foreach (string npcId in _npcIds.Where(id => _provider.KnownNpcIds.Contains(id)))
                return _provider.CreateDefinition(npcId).Fraction;
            return null;
        }

        /// <summary>An enemy of the house does not heal in its camp: the player by faction standing,
        /// NPCs by the static matrix. No relation service or unresolved faction = open door.</summary>
        private bool CanRestHere(IFightable guest)
        {
            if (_factionRelations == null || Fraction is not { } faction) return true;
            if (guest is IPlayer) return !_factionRelations.IsHostileToPlayer(faction);
            if (guest is INpc npc) return !_factionRelations.IsHostile(faction, npc.Fraction);
            return true;
        }
    }
}
