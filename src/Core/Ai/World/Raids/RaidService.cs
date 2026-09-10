namespace Core.Ai.World.Raids
{
    using System;
    using System.Collections.Generic;
    using Data.GameData;
    using Entity;
    using Entity.Components;
    using Enums;
    using Events;
    using Godot;
    using MessageBus;
    using MessageBus.Messages;
    using Newtonsoft.Json;
    using Services;

    /// <summary>
    /// Raids at Hatred standing: once the cooldown is over, every check interval rolls a chance;
    /// on success a squad spawns at the raiding faction's NEAREST spawn site and marches straight
    /// at the player (raiders get no world brain — the raid drives them; physical contact starts
    /// the battle as usual). Raiders live OUTSIDE the population limit (a raid must not silently
    /// starve on a full world) but still occupy it, so spawn points pause until the raid thins out.
    /// Killing raiders is free reputation-wise: the deed floor already ignores Hatred-standing kills.
    /// Survivors leave after the timeout via the final-death channel (population and personal
    /// memory release like for any burned body).
    /// </summary>
    public class RaidService : IRaidService, IGameDataParticipant, Session.ISessionResettable
    {
        private const string RaidNotificationId = "UI_Raid_Started";

        private record ActiveRaider(IFightableNpc Npc, string NpcId);

        private readonly List<ActiveRaider> _raiders = [];
        private readonly IFactionRelationService _relations;
        private readonly IRaidSpawnRegistry _sites;
        private readonly INpcProvider _npcProvider;
        private readonly INpcWorldSpawner _spawner;
        private readonly INpcPopulationService _population;
        private readonly IPlayerAccessor _playerAccessor;
        private readonly IGameEventBus _gameEventBus;
        private readonly IGameMessageBus _messageBus;
        private readonly IRandomNumberGenerator _rnd;
        private readonly Core.World.Spaces.ISpatialQuery _spatial;
        private readonly Core.World.Spaces.BattleSiteRegistry? _battleSites;
        private RaidsData _config = new();
        private Fractions _raidFraction;
        private float _timeLeft;
        private float _checkTimer;

        public RaidService(
            IFactionRelationService relations,
            IRaidSpawnRegistry sites,
            INpcProvider npcProvider,
            INpcWorldSpawner spawner,
            INpcPopulationService population,
            IPlayerAccessor playerAccessor,
            IGameEventBus gameEventBus,
            IGameMessageBus messageBus,
            IRandomNumberGenerator? rnd = null,
            Core.World.Spaces.BattleSiteRegistry? battleSites = null,
            Core.World.Spaces.ISpatialQuery? spatial = null)
        {
            _relations = relations;
            _sites = sites;
            _npcProvider = npcProvider;
            _spawner = spawner;
            _population = population;
            _playerAccessor = playerAccessor;
            _gameEventBus = gameEventBus;
            _messageBus = messageBus;
            _spatial = spatial ?? Core.World.Spaces.NativeSpatialQuery.Instance;
            _battleSites = battleSites;
            _rnd = rnd ?? new DefaultRandomNumberGenerator();
        }

        public bool IsRaidActive => _raiders.Count > 0;

        public float CooldownRemaining { get; set; }

        public IReadOnlyList<string> Catalogs => [DataCatalog.Raids];

        public void Apply(string catalog, GameDataFile file)
        {
            _config = JsonConvert.DeserializeObject<RaidsData>(file.Json)
                      ?? throw new InvalidOperationException("Failed to deserialize raids config");
            CooldownRemaining = _config.InitialDelaySeconds; // a save restore overwrites this later
        }

        /// <summary>Silent reset: raider nodes die with the old scene — no despawn/final-death traffic here.</summary>
        public void ResetSession()
        {
            _raiders.Clear();
            _timeLeft = 0;
            _checkTimer = 0;
            CooldownRemaining = _config.InitialDelaySeconds;
        }

        public void Tick(float delta)
        {
            if (IsRaidActive)
            {
                TickRaid(delta);
                return;
            }

            if (CooldownRemaining > 0)
            {
                CooldownRemaining -= delta;
                return;
            }

            _checkTimer -= delta;
            if (_checkTimer > 0) return;

            _checkTimer = _config.CheckIntervalSeconds;
            TryStartRaid();
        }

        /// <summary>Test seam: the player node's world position (Godot-side); null = unknown, no raid.</summary>
        protected virtual Vector2? GetPlayerPosition() =>
            _playerAccessor.Player is Node2D node ? node.GlobalPosition : null;

        public bool ForceRaid()
        {
            if (IsRaidActive) return false;
            if (_playerAccessor.Player is not { IsAlive: true, IsFighting: false }) return false;
            if (GetPlayerPosition() is not { } playerPosition) return false;

            return TryLaunchRaid(playerPosition);
        }

        private void TryStartRaid()
        {
            if (_playerAccessor.Player is not { IsAlive: true, IsFighting: false }) return;
            if (GetPlayerPosition() is not { } playerPosition) return;
            if (_rnd.RandFloat() >= _config.Chance) return;

            TryLaunchRaid(playerPosition);
        }

        private bool TryLaunchRaid(Vector2 playerPosition)
        {
            var site = PickNearestHatredSite(playerPosition);
            if (site?.Fraction is not { } faction) return false;

            SpawnSquad(site, faction);
            return IsRaidActive;
        }

        private IRaidSpawnSite? PickNearestHatredSite(Vector2 playerPosition)
        {
            IRaidSpawnSite? best = null;
            float bestDistance = float.MaxValue;
            foreach (var site in _sites.All)
            {
                if (!_spatial.SharesSpace(_playerAccessor.Player, site)) continue;
                if (site.Fraction is not { } faction || site.NpcIds.Count == 0) continue;
                if (!_relations.CanRaid(faction)) continue;
                if (_relations.GetPlayerRelation(faction) != RelationLevel.Hatred) continue;

                float distance = site.Position.DistanceSquaredTo(playerPosition);
                if (distance >= bestDistance) continue;
                best = site;
                bestDistance = distance;
            }

            return best;
        }

        private void SpawnSquad(IRaidSpawnSite site, Fractions faction)
        {
            int size = _rnd.RandIntRange(_config.SquadSizeMin, _config.SquadSizeMax);
            for (int i = 0; i < size; i++)
            {
                string npcId = site.NpcIds[_rnd.RandIntRange(0, site.NpcIds.Count - 1)];
                // No world brain: the raid drives the raider at the player itself.
                var definition = _npcProvider.CreateDefinition(npcId) with { World = null };
                var jitter = new Vector2(
                    _rnd.RandFloatRange(-_config.SpawnJitter, _config.SpawnJitter),
                    _rnd.RandFloatRange(-_config.SpawnJitter, _config.SpawnJitter));
                if (_spawner.SpawnAt(definition, site.Position + jitter, site) is not { } raider) continue;

                _population.ReserveOutsideLimit();
                _raiders.Add(new ActiveRaider(raider, definition.NpcId));
            }

            if (_raiders.Count == 0) return;

            _raidFraction = faction;
            _timeLeft = _config.DurationSeconds;
            _gameEventBus.Publish(new RaidStartedEvent(faction, _raiders.Count, site.Position));
            _ = _messageBus.PublishMessageAsync(new SendNotificationMessageMessage(RaidNotificationId, NotificationCategory.Location,
                new Dictionary<string, object?> { ["Faction"] = Localization.Localization.Localize($"Fraction_{faction}") }));
        }

        private void TickRaid(float delta)
        {
            _timeLeft -= delta;

            bool anyAlive = false;
            bool anyFighting = false;
            foreach (var raider in _raiders)
            {
                if (!raider.Npc.IsAlive) continue;
                anyAlive = true;
                if (raider.Npc.IsFighting)
                {
                    anyFighting = true;
                    continue;
                }

                if (raider.Npc is not IWorldAgent agent) continue;
                if (_timeLeft > 0 && GetPursuitTarget(raider.Npc) is { } target)
                    agent.MoveTo(target, _config.MoveSpeed);
                else agent.StopMoving();
            }

            if (_timeLeft > 0 && anyAlive) return;
            if (anyFighting) return; // the last battle plays out before the raid wraps up

            EndRaid();
        }

        private Vector2? GetPursuitTarget(IFightableNpc raider)
        {
            if (_playerAccessor.Player is { IsFighting: true })
            {
                var site = _battleSites?.Current;
                return site != null && site.SpaceId == _spatial.GetSpace(raider) ? site.Position : null;
            }
            return _playerAccessor.Player is { IsAlive: true }
                && _spatial.SharesSpace(raider, _playerAccessor.Player) ? GetPlayerPosition() : null;
        }

        private void EndRaid()
        {
            foreach (var raider in _raiders)
            {
                if (!raider.Npc.IsAlive) continue; // the dead stay as bodies with their normal lifecycles

                // Final death is the single channel every interested system already listens to:
                // the population slot frees, the personal memory forgets, spawn points ignore strangers.
                _gameEventBus.Publish(new NpcFinalDeathEvent(raider.Npc.InstanceId, raider.NpcId, raider.Npc.Position));
                _spawner.Despawn(raider.Npc);
            }

            _raiders.Clear();
            CooldownRemaining = _config.CooldownSeconds;
            _gameEventBus.Publish(new RaidEndedEvent(_raidFraction));
        }
    }
}
