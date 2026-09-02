namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Enums;
    using EquipData;
    using GameData;
    using Schema;

    /// <summary>ByCategory: equipment-category sections ("Weapon"/"Armor"/"Jewellery") whose entries
    /// only serve items of that category; the flat list serves all. Null = no category split.</summary>
    /// <remarks>Id labels the material where it stands and names no record of its own; CategoryId points
    /// back into the same catalog, at the material category the resource draws its base lines from.</remarks>
    public record MaterialData(
        [property: NotARef] string Id,
        [property: CatalogRef(DataCatalog.Resources)] string CategoryId,
        List<ItemModifier> Modifiers,
        [property: DictionaryKey(typeof(EquipmentCategory))] Dictionary<string, List<ItemModifier>>? ByCategory = null);
}
