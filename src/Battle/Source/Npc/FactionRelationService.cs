namespace Battle.Source.Npc
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;
    using Core.Data;
    using Core.Data.FactionData;
    using Core.Entity;
    using Core.Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads FactionRelations.json and answers hostility questions. Directed matrix: an entry
    /// "from → to" does not imply the reverse (undead assault demons, demons don't care).
    /// The player's standing starts from the JSON defaults and changes at runtime.
    /// </summary>
    public class FactionRelationService : IFactionRelationService
    {
        private const string DataPath = "res://Data/Factions/";

        private readonly Dictionary<(Fractions From, Fractions To), RelationLevel> _relations = [];
        private readonly Dictionary<Fractions, RelationLevel> _playerRelations = [];

        /// <summary>Runtime constructor: loads the JSON asynchronously like every provider.</summary>
        public FactionRelationService() => _ = LoadDataAsync();

        /// <summary>Test constructor: applies the data directly, no Godot file access involved.</summary>
        public FactionRelationService(FactionRelationsData data) => ApplyData(data);

        public event Action<Fractions, RelationLevel>? PlayerRelationChanged;

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
                _relations[(ParseEnum<Fractions>(entry.From), ParseEnum<Fractions>(entry.To))] = ParseEnum<RelationLevel>(entry.Level);

            foreach (var entry in data.PlayerDefaults)
                _playerRelations[ParseEnum<Fractions>(entry.Fraction)] = ParseEnum<RelationLevel>(entry.Level);
        }

        private static T ParseEnum<T>(string value) where T : struct, Enum =>
            Enum.TryParse(value, ignoreCase: true, out T result)
                ? result
                : throw new FormatException($"'{value}' is not a valid {typeof(T).Name}");

        private async Task LoadDataAsync()
        {
            try
            {
                await DataLoader.LoadDataFromJson(DataPath, ParseRelations);
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to load faction relations data", e);
            }
        }

        private Task ParseRelations(string json)
        {
            var data = JsonConvert.DeserializeObject<FactionRelationsData>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize faction relations");
            ApplyData(data);
            return Task.CompletedTask;
        }
    }
}
