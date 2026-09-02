namespace Core.Data.LootTable
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record LootTableData
    {
        /// <summary>Which table this is. What the name POINTS AT depends on the section the table
        /// stands in — a fraction, an entity type, one npc's own id, or nothing at all — so the type
        /// carries no markup for it and the catalog descriptor answers once per section.</summary>
        [JsonProperty("key")] public string Key { get; init; } = string.Empty;
        [JsonProperty("tiers")] public List<LootTableTierData> Tiers { get; init; } = [];
    }
}
