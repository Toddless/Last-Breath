namespace Core.Reputation
{
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World.Skirmish;
    using Entity;
    using Events;
    using Godot;

    /// <summary>
    /// Level-0 witnesses over the NPC world registry: alive, not fighting (participants of any
    /// battle are busy, not watching), of a faction that carries reputation (beasts can't talk).
    /// While a battle runs, fighters stand on arena spots — the deed's world anchor is where the
    /// fight started (captured from BattleInitializedEvent, published before the arena transfer).
    /// NPCs that fled the current battle are guaranteed witnesses until the battle ends.
    /// </summary>
    public class WorldWitnessQuery : IWitnessQuery
    {
        private readonly INpcWorldRegistry _registry;
        private readonly IFactionRelationService _relations;
        private readonly HashSet<string> _fled = [];
        private Vector2? _battleSite;
        private ulong _battleSpace;
        private readonly Core.World.Spaces.ISpatialQuery _spatial;
        private readonly Core.Services.IPlayerAccessor? _player;

        public WorldWitnessQuery(INpcWorldRegistry registry, IFactionRelationService relations, IGameEventBus gameEventBus, Core.Services.IPlayerAccessor? player = null, Core.World.Spaces.ISpatialQuery? spatial = null)
        {
            _registry = registry;
            _spatial = spatial ?? Core.World.Spaces.NativeSpatialQuery.Instance;
            _player = player;
            _relations = relations;
            gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleInitialized);
            gameEventBus.Subscribe<EntityFledBattleEvent>(OnEntityFled);
            gameEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        public bool HasWitness(Vector2 position, float radius, string? excludeInstanceId = null)
        {
            if (_fled.Count > 0) return true;

            var anchor = _battleSite ?? position;
            ulong space = _battleSite != null ? _battleSpace : _spatial.GetSpace(_player?.Player);
            if (space == 0) return false;
            foreach (var npc in _registry.All)
            {
                if (_spatial.GetSpace(npc) != space) continue;
                if (npc is not { IsAlive: true, IsFighting: false }) continue;
                if (npc.InstanceId == excludeInstanceId) continue;
                if (!_relations.HasReputation(npc.Fraction)) continue;
                if (npc.Position.DistanceSquaredTo(anchor) <= radius * radius) return true;
            }

            return false;
        }

        private void OnBattleInitialized(BattleInitializedEvent evnt)
        {
            _fled.Clear();
            _battleSite = evnt.Player is Node2D playerNode ? playerNode.GlobalPosition : evnt.Entities.OfType<INpc>().FirstOrDefault()?.Position;
            _battleSpace = _spatial.GetSpace(evnt.Player);
        }

        private void OnEntityFled(EntityFledBattleEvent evnt)
        {
            if (_battleSite == null || !_spatial.SharesSpace(evnt.Entity, _player?.Player)) return;
            if (evnt.Entity is INpc npc && _relations.HasReputation(npc.Fraction))
                _fled.Add(npc.InstanceId);
        }

        private void OnBattleEnd(BattleEndEvent evnt)
        {
            _fled.Clear();
            _battleSite = null;
            _battleSpace = 0;
        }
    }
}
