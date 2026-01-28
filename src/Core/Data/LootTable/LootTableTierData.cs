namespace Core.Data.LootTable
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record LootTableTierData
    {
        [JsonProperty("tier")] public int Tier { get; init; }
        [JsonProperty("items")] public List<TableRecord> Items { get; init; } = [];
    };
}
