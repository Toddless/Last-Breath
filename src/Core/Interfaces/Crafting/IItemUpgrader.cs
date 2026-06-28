namespace Core.Interfaces.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Items;
    using Modifiers;
    using Results;

    public interface IItemUpgrader
    {
        List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
