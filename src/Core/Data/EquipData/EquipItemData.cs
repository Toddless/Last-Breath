namespace Core.Data.EquipData
{
    using System.Collections.Generic;

    public record EquipItemData(
        string Id,
        string EquipmentPart,
        int MaxStackSize,
        string Rarity,
        string[] Tags,
        string AttributeType,
        string EffectId,
        int UpdateLevel,
        int MaxUpdateLevel,
        List<ItemModifier> BaseModifiers,
        List<ItemModifier> AdditionalModifiers);
}
