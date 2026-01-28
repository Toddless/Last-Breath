namespace Core.Data.CraftingData
{
    using System.Collections.Generic;

    public record CraftingRecipeData(
        string Id,
        string ResultItemId,
        string[] Tags,
        string Rarity,
        bool IsOpened,
        List<ResourceRequirementData> MainResources);
}
