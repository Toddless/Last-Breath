namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using EquipData;

    public record MaterialCategoryData(string Id, List<ItemModifier> Modifiers);
}
