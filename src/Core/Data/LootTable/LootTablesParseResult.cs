namespace Core.Data.LootTable
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>Loot tables parsed from one JSON document; the provider merges files into its lookup.</summary>
    public sealed class LootTablesParseResult
    {
        public Dictionary<Fractions, List<LootTableTierData>> FractionTables { get; } = [];
        public Dictionary<EntityType, List<LootTableTierData>> EntityTypeTables { get; } = [];
        public Dictionary<string, List<LootTableTierData>> IndividualTables { get; } = [];
        public List<LootTableTierData> BasicTable { get; } = [];
    }
}
