namespace Core.Data.LootTable
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record LootTableData
    {
        [JsonProperty("key")] public string Key { get; init; } = string.Empty;
        [JsonProperty("tiers")] public List<LootTableTierData> Tiers { get; init; } = [];
    }
}
