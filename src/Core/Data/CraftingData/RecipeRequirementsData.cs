namespace Core.Data.CraftingData
{
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    public record RecipeRequirementsData
    {
        [JsonProperty("type")][EnumOf(typeof(RequirementType))] public string Type { get; set; } = string.Empty;

        /// <summary>What the line is paid with: one crafting resource, or a whole category of materials —
        /// a Resource line whose id names a category is read as the category, so both sections answer the
        /// same field.</summary>
        /// <remarks>Under <see cref="RequirementType.MasteryLevel"/> the id is a localization key instead:
        /// the gate reads the amount alone, and the window words the line by this string.</remarks>
        [JsonProperty("id")]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.MaterialCategoriesSection)]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("amount")] public int Amount { get; set; }
    }
}
