namespace Battle.Internal.Npc
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Events.GameEvents;
    using Source;
    using Core.Services;
    using Godot;
    using GameServiceProvider = Services.GameServiceProvider;

    /// <summary>
    /// A faction's home on the map (forest/camp/cemetery/city): keeps its population at
    /// <see cref="_maxCount"/>, spawning from its Npc.json id list at free spots in the radius.
    /// A burned body (final death) or a body risen into another faction frees the slot and
    /// schedules a replacement; the global cap is enforced by <see cref="INpcPopulationService"/>.
    /// </summary>
    [GlobalClass]
    internal partial class NpcSpawnPoint : Node2D
    {
        [Export] private string[] _npcIds = [];
        [Export] private int _maxCount = 3;
        [Export] private float _spawnRadius = 200f;
        [Export] private bool _spawnAsGroup;
        [Export] private float _respawnDelaySeconds = 10f;
        [Export] private bool _spawnOnReady = true;

        private readonly HashSet<string> _ownedInstanceIds = [];
        private readonly RandomNumberGenerator _rnd = new();
        private IGameServiceProvider _gameServiceProvider;
        private INpcProvider? _provider;
        private IGameEventBus? _gameEventBus;
        private INpcPopulationService? _population;
        private EntityGroup? _group;
        private int _pendingRespawns;
        private float _respawnTimer;

        public override void _Ready()
        {
            _rnd.Randomize();
            _gameServiceProvider = GameServiceProvider.Instance;
            _provider = _gameServiceProvider.GetService<INpcProvider>();
            _gameEventBus = _gameServiceProvider.GetService<IGameEventBus>();
            _population = _gameServiceProvider.GetService<INpcPopulationService>();
            _gameEventBus?.Subscribe<NpcFinalDeathEvent>(OnFinalDeath);
            _gameEventBus?.Subscribe<NpcFactionChangedEvent>(OnFactionChanged);

            // Deferred so the providers finish loading their JSON before the first spawn.
            if (_spawnOnReady) CallDeferred(nameof(FillToCapacity));
        }

        public override void _ExitTree()
        {
            _gameEventBus?.Unsubscribe<NpcFinalDeathEvent>(OnFinalDeath);
            _gameEventBus?.Unsubscribe<NpcFactionChangedEvent>(OnFactionChanged);
        }

        public override void _Process(double delta)
        {
            if (_pendingRespawns <= 0) return;

            _respawnTimer -= (float)delta;
            if (_respawnTimer > 0) return;

            _respawnTimer = _respawnDelaySeconds;
            if (TrySpawnOne()) _pendingRespawns--;
        }

        private void FillToCapacity()
        {
            while (_ownedInstanceIds.Count < _maxCount && TrySpawnOne())
            {
            }
        }

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
                world.AddChild(npc); // _Ready builds the components ApplyDefinition configures
                npc.GlobalPosition = RollSpotInRadius();
                npc.ApplyDefinition(definition);
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
            if (_pendingRespawns == 0) _respawnTimer = _respawnDelaySeconds;
            _pendingRespawns++;
        }
    }
}
