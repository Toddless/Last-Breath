namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using EquipData;

    /// <summary>ByCategory: equipment-category sections ("Weapon"/"Armor"/"Jewellery") whose entries
    /// only serve items of that category; the flat list serves all. Null = no category split.</summary>
    public record MaterialCategoryData(string Id, List<ItemModifier> Modifiers, Dictionary<string, List<ItemModifier>>? ByCategory = null);
}
