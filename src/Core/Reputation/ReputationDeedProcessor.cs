namespace Core.Reputation
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.GameData;
    using Data.ReputationData;
    using Entity;
    using Enums;
    using Events;
    using Events.GameEvents;
    using Godot;
    using Newtonsoft.Json;
    using Save;
    using Services;

    /// <summary>
    /// The single place deeds turn into reputation: subscribes to the game bus and applies the
    /// deltas from ReputationDeeds.json. Kills are derived from EntityDiedEvent (only when the
    /// killer is the player); everything else arrives as an explicit PlayerDeedEvent. Applies
    /// the no-penalty floor (already-hostile targets are fair game), the hostile-to-target
    /// bonus through the static matrix, and a session-scoped repeat decay against farming.
    /// </summary>
    public class ReputationDeedProcessor : IGameDataParticipant, IReputationDeedProcessor
    {
        private record Deed(ReputationDeedEntry Entry, RelationLevel? NoPenaltyFloor);

        private readonly Dictionary<string, Deed> _deeds = [];
        private readonly Dictionary<(string DeedId, Fractions Faction), int> _repeats = [];
        private readonly IFactionRelationService _relations;
        private readonly IPlayerAccessor _playerAccessor;
        private readonly ILoadScope _loadScope;
        private readonly IWitnessQuery _witnesses;
        private readonly IPersonalReputationService _personal;
        private float _witnessRadius;

        public ReputationDeedProcessor(IGameEventBus gameEventBus, IFactionRelationService relations, IPlayerAccessor playerAccessor, ILoadScope loadScope, IWitnessQuery witnesses, IPersonalReputationService personal)
        {
            _relations = relations;
            _playerAccessor = playerAccessor;
            _loadScope = loadScope;
            _witnesses = witnesses;
            _personal = personal;
            gameEventBus.Subscribe<EntityDiedEvent>(OnEntityDied);
            gameEventBus.Subscribe<PlayerDeedEvent>(OnPlayerDeed);
        }

        public IReadOnlyList<string> Catalogs => [DataCatalog.ReputationDeeds];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<ReputationDeedsData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize reputation deeds");
            _witnessRadius = data.WitnessRadius;

            // Enums are parsed here so a typo is a load-time report, not a mid-game throw.
            foreach (var deed in data.Deeds)
                _deeds[deed.Id] = new Deed(deed,
                    deed.NoPenaltyAtOrBelow == null ? null : EnumParser.ParseEnum<RelationLevel>(deed.NoPenaltyAtOrBelow));
        }

        private void OnEntityDied(EntityDiedEvent evnt)
        {
            if (_loadScope.IsLoading) return; // restored bodies drop health through the normal property
            // TODO:
            // Точно ли я хочу сравнивать по референсу? Возможно стоит сменить на Id игрока.
            if (evnt.Killer == null || !ReferenceEquals(evnt.Killer, _playerAccessor.Player)) return;
            if (evnt.Entity is not INpc npc) return;

            ApplyDeed(DeedIds.KillNpc, npc.Fraction, npc.InstanceId, npc.Position);
        }

        private void OnPlayerDeed(PlayerDeedEvent evnt)
        {
            if (_loadScope.IsLoading) return;
            ApplyDeed(evnt.DeedId, evnt.TargetFaction, evnt.TargetInstanceId, evnt.Position);
        }

        private void ApplyDeed(string deedId, Fractions target, string? targetInstanceId, Vector2 position)
        {
            if (!_deeds.TryGetValue(deedId, out var deed))
            {
                Tracker.TrackNotFound($"Reputation deed '{deedId}' is not in ReputationDeeds.json");
                return;
            }

            // Nobody learned about it — nothing happened, for the penalty and the bonuses alike.
            if (deed.Entry.RequiresWitness && !_witnesses.HasWitness(position, _witnessRadius, targetInstanceId)) return;

            if (deed.Entry.Personal != 0 && targetInstanceId != null)
                _personal.AddPersonal(targetInstanceId, deed.Entry.Personal, deed.Entry.Id);

            ApplyToFaction(deed, target, MainDelta(deed, target));

            if (deed.Entry.HostileToTargetBonus == 0) return;
            foreach (Fractions other in Enum.GetValues<Fractions>())
                if (other != target && _relations.IsHostile(other, target))
                    ApplyToFaction(deed, other, deed.Entry.HostileToTargetBonus);
        }

        /// <summary>The floor: a penalty against an already-hostile faction costs nothing (self-defense, undead).</summary>
        private int MainDelta(Deed deed, Fractions target)
        {
            if (deed.Entry.Reputation >= 0 || deed.NoPenaltyFloor == null) return deed.Entry.Reputation;
            return _relations.GetPlayerRelation(target) <= deed.NoPenaltyFloor ? 0 : deed.Entry.Reputation;
        }

        /// <summary>Each repetition against the same faction shrinks the delta; the counter is per session.</summary>
        private void ApplyToFaction(Deed deed, Fractions faction, int delta)
        {
            int repeats = _repeats.GetValueOrDefault((deed.Entry.Id, faction));
            _repeats[(deed.Entry.Id, faction)] = repeats + 1;
            if (delta == 0) return;

            int decayed = (int)Math.Round(delta * Math.Pow(deed.Entry.RepeatDecay, repeats));
            _relations.AddReputation(faction, decayed, deed.Entry.Id);
        }
    }
}
