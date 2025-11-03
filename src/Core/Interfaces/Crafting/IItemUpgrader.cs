namespace Core.Interfaces.Crafting
{
    using Core.Enums;
    using Core.Results;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using System.Collections.Generic;
    using Core.Interfaces.Entity;

    public interface IItemUpgrader
    {
        List<IResourceRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IResourceRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory, ItemUpgradeMode mode);
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IMaterialModifier> modifiers, IEntity? player = null);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
