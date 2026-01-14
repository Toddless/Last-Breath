namespace Core.Interfaces.Crafting
{
    using Enums;
    using Results;
    using Interfaces;
    using Items;
    using System.Collections.Generic;
    using Entity;

    public interface IItemUpgrader
    {
        List<IResourceRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IResourceRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory, ItemUpgradeMode mode);
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IMaterialModifier> modifiers, IEntity? player = null);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
