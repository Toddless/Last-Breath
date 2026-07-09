namespace LastBreath.Npc
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Data.FactionData;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads FactionRelations.json and answers hostility questions. Directed matrix: an entry
    /// "from → to" does not imply the reverse (undead assault demons, demons don't care).
    /// The player's standing starts from the JSON defaults and changes at runtime.
    /// </summary>
    public class FactionRelationService : IFactionRelationService, IGameDataParticipant
    {
        private readonly Dictionary<(Fractions From, Fractions To), RelationLevel> _relations = [];
        private readonly Dictionary<Fractions, RelationLevel> _playerRelations = [];

        public FactionRelationService()
        {
        }

        /// <summary>Test constructor: applies the data directly, no file access involved.</summary>
        public FactionRelationService(FactionRelationsData data) => ApplyData(data);

        public event Action<Fractions, RelationLevel>? PlayerRelationChanged;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Factions];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<FactionRelationsData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize faction relations");
            ApplyData(data);
        }

        public RelationLevel GetRelation(Fractions from, Fractions to)
        {
            if (from == to) return RelationLevel.Alliance;
            return _relations.GetValueOrDefault((from, to), RelationLevel.Neutral);
        }

        public bool IsHostile(Fractions from, Fractions to) => GetRelation(from, to) <= RelationLevel.Hostility;

        public RelationLevel GetPlayerRelation(Fractions faction) =>
            _playerRelations.GetValueOrDefault(faction, RelationLevel.Neutral);

        public void SetPlayerRelation(Fractions faction, RelationLevel level)
        {
            if (GetPlayerRelation(faction) == level) return;
            _playerRelations[faction] = level;
            PlayerRelationChanged?.Invoke(faction, level);
        }

        public bool IsHostileToPlayer(Fractions faction) => GetPlayerRelation(faction) <= RelationLevel.Hostility;

        private void ApplyData(FactionRelationsData data)
        {
            foreach (var entry in data.Relations)
                _relations[(EnumParser.ParseEnum<Fractions>(entry.From), EnumParser.ParseEnum<Fractions>(entry.To))] = EnumParser.ParseEnum<RelationLevel>(entry.Level);

            foreach (var entry in data.PlayerDefaults)
                _playerRelations[EnumParser.ParseEnum<Fractions>(entry.Fraction)] = EnumParser.ParseEnum<RelationLevel>(entry.Level);
        }
    }
}
