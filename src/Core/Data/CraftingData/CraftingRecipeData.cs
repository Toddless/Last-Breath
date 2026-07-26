namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record CraftingRecipeData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("resultItemId")] public string ResultItemId { get; init; } = string.Empty;
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];
        [JsonProperty("rarity")] public string Rarity { get; init; } = string.Empty;
        [JsonProperty("unlockAtMastery")] public int? UnlockAtMastery { get; init; }
        [JsonProperty("basePrice")] public int BasePrice { get; init; }
        [JsonProperty("itemType")] public string ItemType { get; init; } = string.Empty;
        [JsonProperty("requirements")] public List<RecipeRequirementsData> Requirements { get; init; } = [];
        [JsonProperty("optionalResourceCategories")] public string[] OptionalResourceCategories { get; init; } = [];
    }
}
