namespace LastBreath.World
{
    using System;
    using Core.Ai.World.Spawn;
    using Core.Data;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Narrative.Facts;
    using Core.Save;
    using Core.Services;
    using Godot;
    using Npc;

    /// <summary>
    /// A boss's lair. Two categories (Боссы.md → Механика → Респавн боссов):
    /// Single — spawns unless the final-death world fact is set, never returns after it;
    /// FactionDeaths — returns after every N combat deaths of his faction (counter in WorldFacts,
    /// FROZEN while the boss lives, final-death fact deliberately ignored). Bosses reserve
    /// population OUTSIDE the global cap (raid pattern) — a crowded world never blocks a boss.
    /// Alive state persists like NpcSpawnPoint's (IPersistentSpawnPoint); the death counter
    /// persists with the narrative save on its own.
    /// </summary>
    [GlobalClass]
    internal partial class BossSpawnPoint : Node2D, IPersistentSpawnPoint
    {
        [Export] private string _bossId = "";
        [Export] private BossRespawnMode _respawnMode = BossRespawnMode.Single;
        [Export] private int _factionDeathsPerRespawn = 200;

        /// <summary>Stable save identity; empty = the scene-tree path (fine until the node is renamed/moved).</summary>
        [Export] private string _pointId = "";

        private IGameServiceProvider _gameServiceProvider;
        private INpcProvider? _provider;
        private IGameEventBus? _gameEventBus;
        private INpcPopulationService? _population;
        private ISpawnPointRegistry? _spawnRegistry;
        private IWorldFactsService? _facts;
        private BossRespawnTracker? _tracker;
        private string? _bossInstanceId;
        private bool _bossAlive;
        private bool _spawnPending;

        public string PointId => string.IsNullOrEmpty(_pointId) ? Locations.LocationRoot.Find(this)?.GetPathTo(this).ToString() ?? GetPath().ToString() : _pointId;

        public override void _Ready()
        {
            _gameServiceProvider = Core.Services.GameServiceProvider.Instance;
            _provider = _gameServiceProvider.GetService<INpcProvider>();
            _gameEventBus = _gameServiceProvider.GetService<IGameEventBus>();
            _population = _gameServiceProvider.GetService<INpcPopulationService>();
            _spawnRegistry = _gameServiceProvider.GetService<ISpawnPointRegistry>();
            _facts = _gameServiceProvider.GetService<IWorldFactsService>();
            _spawnRegistry?.Register(this);
            _gameEventBus?.Subscribe<EntityDiedEvent>(OnEntityDied);
            _gameEventBus?.Subscribe<NpcFinalDeathEvent>(OnFinalDeath);
            _gameEventBus?.Subscribe<NpcFactionChangedEvent>(OnFactionChanged);

            // A pending load owns the initial population (same policy as NpcSpawnPoint).
            bool loadPending = _gameServiceProvider.GetService<ISaveGameService>()?.HasPendingLoad == true;
            // Deferred so the providers finish loading their JSON before the first spawn.
            if (!loadPending && Locations.LocationRoot.Find(this)?.IsPreparing != true) CallDeferred(nameof(SpawnIfAllowed));
        }

        public override void _ExitTree()
        {
            _spawnRegistry?.Unregister(this);
            _gameEventBus?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
            _gameEventBus?.Unsubscribe<NpcFinalDeathEvent>(OnFinalDeath);
            _gameEventBus?.Unsubscribe<NpcFactionChangedEvent>(OnFactionChanged);
        }

        /// <summary>Deaths resolve inside battles (physics/replay callbacks) — the actual spawn is
        /// deferred to _Process, the same "never touch the tree from a combat callback" rule.</summary>
        public override void _Process(double delta)
        {
            if (Locations.LocationRoot.Find(this)?.IsPreparing == true || !_spawnPending) return;
            _spawnPending = false;
            SpawnBoss();
        }

        public SpawnPointSaveData CaptureState() => new()
        {
            Id = PointId,
            Alive = _bossAlive ? 1 : 0,
            PendingDueMinutes = [],
        };

        public void RestoreState(SpawnPointSaveData data)
        {
            if (data.Alive > 0) SpawnBoss();
        }

        public void FillFresh() => SpawnIfAllowed();
        public string? OwnedId => _bossInstanceId;
        public void RestoreOwnership(BaseNpc? npc)
        {
            _bossInstanceId = npc?.InstanceId;
            EnsureTrackerFromDefinition();
            SetBossAlive(npc?.IsAlive == true);
            _spawnPending = !_bossAlive && _respawnMode == BossRespawnMode.FactionDeaths && _tracker?.IsRespawnDue == true;
        }

        /// <summary>First appearance: both categories spawn on a fresh world; Single never returns
        /// after the final-death fact, FactionDeaths ignores it by design (Todd, 2026-07-17).</summary>
        private void SpawnIfAllowed()
        {
            if (_bossAlive || string.IsNullOrEmpty(_bossId)) return;
            if (_respawnMode == BossRespawnMode.Single && _facts?.IsSet(FactKeys.NpcFinalDeath(_bossId)) == true) return;
            SpawnBoss();
        }

        private void SpawnBoss()
        {
            if (_bossAlive || string.IsNullOrEmpty(_bossId)) return;
            if (GetParent() is not Node2D world) return;

            try
            {
                var definition = _provider!.CreateDefinition(_bossId);
                EnsureTracker(definition.Fraction);

                var npc = BaseNpc.Initialize().Instantiate<BaseNpc>();
                npc.InjectServices(_gameServiceProvider);
                // Position BEFORE AddChild — the (0,0) teleport drag pitfall (see NpcSpawnPoint).
                npc.Position = world.ToLocal(GlobalPosition);
                world.AddChild(npc);
                npc.ApplyDefinition(definition, _gameServiceProvider);

                _population?.ReserveOutsideLimit();
                _bossInstanceId = npc.InstanceId;
                SetBossAlive(true);
                _tracker?.ConsumeRespawn();
            }
            catch (Exception e)
            {
                GD.PrintErr($"BossSpawnPoint: spawn of '{_bossId}' failed: {e.Message}");
            }
        }

        private void OnEntityDied(EntityDiedEvent evnt)
        {
            if (evnt.Entity.InstanceId == _bossInstanceId)
            {
                SetBossAlive(false);
                return; // the boss's own death never feeds his own comeback
            }

            if (_respawnMode != BossRespawnMode.FactionDeaths) return;
            if (evnt.Entity is not IFightableNpc npc) return;

            EnsureTrackerFromDefinition();
            _tracker?.OnDeath(npc.Fraction, npc.IsSummon);
            if (_tracker?.IsRespawnDue == true) _spawnPending = true;
        }

        /// <summary>The corpse is gone for good (burned/despawned) — drop the reference; Single mode
        /// is closed forever by the final-death fact the narrative tracker already wrote.</summary>
        private void OnFinalDeath(NpcFinalDeathEvent evnt)
        {
            if (evnt.InstanceId != _bossInstanceId) return;
            _bossInstanceId = null;
            SetBossAlive(false);
        }

        /// <summary>Our boss rose as undead: same instance, walking again — the counter refreezes.</summary>
        private void OnFactionChanged(NpcFactionChangedEvent evnt)
        {
            if (evnt.InstanceId != _bossInstanceId) return;
            SetBossAlive(true);
        }

        private void SetBossAlive(bool alive)
        {
            _bossAlive = alive;
            if (_tracker != null) _tracker.BossAlive = alive;
        }

        /// <summary>The tracker needs the boss's faction, which lives in the definition — resolved
        /// lazily so _Ready never races the JSON providers.</summary>
        private void EnsureTrackerFromDefinition()
        {
            if (_tracker != null || _provider == null || _facts == null) return;
            try
            {
                EnsureTracker(_provider.CreateDefinition(_bossId).Fraction);
            }
            catch (Exception e)
            {
                GD.PrintErr($"BossSpawnPoint: definition of '{_bossId}' failed: {e.Message}");
            }
        }

        private void EnsureTracker(Fractions faction)
        {
            if (_tracker != null || _facts == null) return;
            _tracker = new BossRespawnTracker(_facts, _bossId, faction, _factionDeathsPerRespawn) { BossAlive = _bossAlive };
        }
    }
}
