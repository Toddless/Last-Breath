namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using EquipData;

    public record MaterialData(string Id, string CategoryId, List<ItemModifier> Modifiers);
}
