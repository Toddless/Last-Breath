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
        [JsonProperty("requirements")] public List<RecipeRequirementsData> Requirements { get; init; } = [];
    }
}
