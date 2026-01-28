namespace Core.Data.CraftingData
{
    using System.Collections.Generic;

    public record MaterialCategoryData(string Id, List<MaterialModifierData> Modifiers);
}
