namespace Core.Interfaces.Crafting
{
    using Enums;
    using Results;
    using Items;
    using System.Collections.Generic;
    using Entity;
    using Modifiers;

    public interface IItemUpgrader
    {
        List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory, ItemUpgradeMode mode);
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers, IEntity? player = null);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
