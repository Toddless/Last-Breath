namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>The three sections of the resources file. The keys are named here rather than only in the
    /// authoring tool's descriptor: a reference narrowed to one section is written on a DTO, and the game
    /// cannot see the descriptor. Each section is read under its own constant rather than under whatever
    /// the naming strategy makes of the member — a member renamed would otherwise move the key and leave
    /// the constant behind, in silence.</summary>
    public record ResourcesData(
        [property: JsonProperty(ResourcesData.MaterialCategoriesSection)] List<MaterialCategoryData> MaterialCategories,
        [property: JsonProperty(ResourcesData.UpgradeResourcesSection)] List<UpgradeResourceData> UpgradeResources,
        [property: JsonProperty(ResourcesData.CraftingResourcesSection)] List<CraftingResourceData> CraftingResources)
    {
        /// <summary>Json key of the section holding the categories a material draws its base lines from.</summary>
        public const string MaterialCategoriesSection = "materialCategories";

        /// <summary>Json key of the section holding the resources sharpening and reforging are paid with.</summary>
        public const string UpgradeResourcesSection = "upgradeResources";

        /// <summary>Json key of the section holding the resources an item is crafted out of.</summary>
        public const string CraftingResourcesSection = "craftingResources";
    }
}
