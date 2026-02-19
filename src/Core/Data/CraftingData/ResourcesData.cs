namespace Core.Data.CraftingData
{
    using System.Collections.Generic;

    public record ResourcesData(
        List<MaterialCategoryData> MaterialCategories,
        List<UpgradeResourceData> UpgradeResources,
        List<CraftingResourceData> CraftingResources);
}
