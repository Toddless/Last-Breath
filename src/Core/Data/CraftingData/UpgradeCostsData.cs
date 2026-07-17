namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record UpgradeCostsData
    {
        [JsonProperty("upgrade")] public List<CategoryRequirementsData> Upgrade { get; init; } = [];
        [JsonProperty("recraft")] public List<CategoryRequirementsData> Recraft { get; init; } = [];
        [JsonProperty("ascend")] public List<CategoryRequirementsData> Ascend { get; init; } = [];
    }

    public record CategoryRequirementsData
    {
        [JsonProperty("category")] public string Category { get; init; } = string.Empty;
        [JsonProperty("requirements")] public List<UpgradeRequirementData> Requirements { get; init; } = [];
    }

    /// <summary>One cost line: id/amount are the defaults for every rarity; byRarity overrides either
    /// half per item rarity (rarity dimension of the upgrade/recraft cost schema).</summary>
    public record UpgradeRequirementData
    {
        [JsonProperty("type")] public string Type { get; init; } = string.Empty;
        [JsonProperty("id")] public string? Id { get; init; }
        [JsonProperty("amount")] public int? Amount { get; init; }
        [JsonProperty("byRarity")] public Dictionary<string, RarityRequirementOverrideData>? ByRarity { get; init; }
    }

    public record RarityRequirementOverrideData
    {
        [JsonProperty("id")] public string? Id { get; init; }
        [JsonProperty("amount")] public int? Amount { get; init; }
    }
}
