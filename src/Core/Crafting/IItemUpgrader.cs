namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Results;
    using Interfaces;
    using Items;

    public interface IItemUpgrader
    {
        List<IRequirement> GetRecraftResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);
        List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);

        /// <summary>Returns the new line's InstanceId, or null when the target line does not exist or the pool
        /// is exhausted — a blind reroll would add a modifier without removing one. Handles both entity and
        /// context lines. <paramref name="additiveResourceIds"/>: optional resources spent on the operation;
        /// their recraft pools join the roll for this call only.</summary>
        string? TryRecraftModifier(IEquipItem item, string modifierInstanceId, IReadOnlyCollection<string>? additiveResourceIds = null);

        /// <summary>Additives raise the success chance and may grant a second level on success.</summary>
        ItemUpgradeResult TryUpgradeItem(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null);
    }
}
