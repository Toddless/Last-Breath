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
        /// <summary>Null when the hash targets nothing rerollable (context lines, stale hashes) —
        /// a blind reroll would add a modifier without removing one.</summary>
        IModifierInstance? TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers);
        ItemUpgradeResult TryUpgradeItem(IEquipItem item);
    }
}
