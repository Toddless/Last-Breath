namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Enums;

    public class LootTableProvider(IDataParser dataParser) : ILootTableProvider, IGameDataParticipant
    {
        private readonly Dictionary<Fractions, List<LootTableTierData>> _fractionTables = [];
        private readonly Dictionary<EntityType, List<LootTableTierData>> _entityTypeTables = [];
        private readonly Dictionary<string, List<LootTableTierData>> _individualTables = [];
        private readonly List<LootTableTierData> _basicTable = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.LootTables];

        public List<LootTableTierData> BasicTable => _basicTable.ToList();

        public void Apply(string catalog, GameDataFile file)
        {
            var parsed = dataParser.ParseLootTables(file.Json);

            _basicTable.AddRange(parsed.BasicTable);
            foreach ((Fractions key, var tiers) in parsed.FractionTables)
                _fractionTables.TryAdd(key, tiers);
            foreach ((EntityType key, var tiers) in parsed.EntityTypeTables)
                _entityTypeTables.TryAdd(key, tiers);
            foreach ((string key, var tiers) in parsed.IndividualTables)
                _individualTables.TryAdd(key, tiers);
        }

        public List<LootTableTierData> GetLootTable<TKey>(TKey key)
        {
            List<LootTableTierData>? table = key switch
            {
                Fractions fractions => _fractionTables.GetValueOrDefault(fractions),
                EntityType entityType => _entityTypeTables.GetValueOrDefault(entityType),
                string individual => _individualTables.GetValueOrDefault(individual),
                _ => null
            };

            return table != null ? table.ToList() : [];
        }
    }
}
