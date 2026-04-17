namespace Core.Interfaces.Crafting
{
    using Items;
    using Enums;
    using Results;
    using Modifiers;
    using System.Collections.Generic;

    public interface IItemUpgrader
    {
        List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
