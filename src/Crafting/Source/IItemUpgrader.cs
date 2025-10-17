namespace Crafting.Source
{
    using Core.Enums;
    using Core.Results;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public interface IItemUpgrader
    {
        List<IResourceRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IResourceRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory, ItemUpgradeMode mode);
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IMaterialModifier> modifiers, ICharacter? player = null);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
