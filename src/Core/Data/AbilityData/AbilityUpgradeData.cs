namespace Core.Data.AbilityData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record AbilityUpgradeData
    {
         [JsonProperty("id")] public string Id { get; init; } = string.Empty;
         [JsonProperty("tags")] public string[] Tags { get; init; } = [];
         [JsonProperty("tier")] public int Tier { get; init; }
         [JsonProperty("upgradeProperties")] public Dictionary<string, float> UpgradeProperties { get; init; } = [];
    }
}
