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

        /// <summary>Null when the hash targets nothing rerollable (context lines, stale hashes,
        /// an exhausted pool) — a blind reroll would add a modifier without removing one.
        /// <paramref name="additiveResourceIds"/>: optional resources spent on the operation;
        /// their recraft pools join the roll for this call only.</summary>
        IModifierInstance? TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IModifier> modifiers, IReadOnlyCollection<string>? additiveResourceIds = null);

        /// <summary>Additives raise the success chance and may grant a second level on success.</summary>
        ItemUpgradeResult TryUpgradeItem(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null);
    }
}
