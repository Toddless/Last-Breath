namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Results;
    using Interfaces;
    using Items;

    public interface IItemUpgrader
    {
        List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
