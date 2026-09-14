namespace Core.World.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.GameData;
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public record AuthoredChestItem(string SlotId, string ItemId, int Amount, Rarity Rarity = Rarity.Common);
    public record ChestDefinition(string Id, string NameKey, double EmptyRemovalDelayMinutes, List<AuthoredChestItem> Items);
    public sealed class ChestCatalog : IGameDataParticipant
    {
        private readonly Dictionary<string, ChestDefinition> _definitions = [];
        public IReadOnlyList<string> Catalogs => ["Chests"];
        public ChestDefinition Get(string id) => _definitions.TryGetValue(id, out var definition)
            ? definition : throw new InvalidOperationException($"Unknown chest definition '{id}'.");

        public void Apply(string catalog, GameDataFile file)
        {
            var records = JsonConvert.DeserializeObject<List<ChestDefinition>>(file.Json, new StringEnumConverter { AllowIntegerValues = false })
                ?? throw new InvalidOperationException("Empty chest catalog.");
            foreach (var record in records) Validate(record);
            if (records.Select(x => x.Id).Distinct().Count() != records.Count || records.Any(x => _definitions.ContainsKey(x.Id)))
                throw new InvalidOperationException("Duplicate chest definition.");
            foreach (var record in records) _definitions.Add(record.Id, record);
        }

        public static void Validate(ChestDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.NameKey)
                || !double.IsFinite(definition.EmptyRemovalDelayMinutes) || definition.EmptyRemovalDelayMinutes < 0
                || definition.Items == null || definition.Items.Count == 0)
                throw new InvalidOperationException("Invalid chest definition.");
            if (definition.Items.Any(x => string.IsNullOrWhiteSpace(x.SlotId) || string.IsNullOrWhiteSpace(x.ItemId) || x.Amount <= 0)
                || definition.Items.Select(x => x.SlotId).Distinct().Count() != definition.Items.Count)
                throw new InvalidOperationException("Chest slots require unique IDs, item IDs and positive amounts.");
        }
    }
}
