namespace Core.Reputation
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.FactionData;
    using Data.GameData;
    using Entity;
    using Enums;
    using Events;
    using Events.GameEvents;
    using Newtonsoft.Json;

    /// <summary>
    /// Per-NPC opinion points over the faction standing. Scale config comes from the
    /// personalReputation section of FactionRelations.json (a second participant on the same
    /// catalog). Cleanup is event-driven: a burned body or a body risen as undead forgets.
    /// </summary>
    public class PersonalReputationService : IPersonalReputationService, IGameDataParticipant
    {
        private readonly Dictionary<string, int> _points = [];
        private readonly IFactionRelationService _factions;
        private PersonalReputationData _config = new();

        public PersonalReputationService(IFactionRelationService factions, IGameEventBus gameEventBus)
        {
            _factions = factions;
            gameEventBus.Subscribe<NpcFinalDeathEvent>(evnt => Forget(evnt.InstanceId));
            gameEventBus.Subscribe<NpcFactionChangedEvent>(evnt => Forget(evnt.InstanceId));
        }

        public IReadOnlyDictionary<string, int> Snapshot => _points;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Factions];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<FactionRelationsData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize faction relations");
            _config = data.PersonalReputation;
        }

        public int GetPersonal(string instanceId) => _points.GetValueOrDefault(instanceId);

        public void AddPersonal(string instanceId, int delta, string reason)
        {
            if (delta == 0) return;
            SetPersonal(instanceId, GetPersonal(instanceId) + delta);
        }

        public void SetPersonal(string instanceId, int points) =>
            _points[instanceId] = Math.Clamp(points, _config.Min, _config.Max);

        public int GetLevelShift(string instanceId) =>
            Math.Clamp(GetPersonal(instanceId) / _config.PointsPerShift, -_config.MaxShift, _config.MaxShift);

        public RelationLevel GetEffectiveRelation(string instanceId, Fractions faction)
        {
            int shifted = (int)_factions.GetPlayerRelation(faction) + GetLevelShift(instanceId);
            return (RelationLevel)Math.Clamp(shifted, (int)RelationLevel.Hatred, (int)RelationLevel.Alliance);
        }

        public bool IsHostileToPlayer(string instanceId, Fractions faction) =>
            GetEffectiveRelation(instanceId, faction) <= RelationLevel.Hostility;

        public void Forget(string instanceId) => _points.Remove(instanceId);
    }
}
