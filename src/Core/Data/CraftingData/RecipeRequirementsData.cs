namespace Core.Data.CraftingData
{
    using Newtonsoft.Json;

    public record RecipeRequirementsData
    {
        [JsonProperty("type")] public string Type { get; set; } = string.Empty;
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;
        [JsonProperty("amount")] public int Amount { get; set; }
    }
}
