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

        /// <summary>The ACTUAL recraft price of this item: the base cost of its rarity/category with every
        /// amount scaled by the reroll-count growth (ceil). The handler computes and spends this — any
        /// displayed price is a mirror of it.</summary>
        List<IRequirement> GetRecraftResourceCost(Items.IEquipItem item);
        List<IRequirement> GetUpgradeResourceCost(Rarity itemRarity, EquipmentCategory itemCategory);

        /// <summary>Returns the new line's InstanceId, or null when the target line does not exist or the pool
        /// is exhausted — a blind reroll would add a modifier without removing one. Handles both entity and
        /// context lines. <paramref name="additiveResourceIds"/>: optional resources spent on the operation;
        /// their recraft pools join the roll for this call only.</summary>
        string? TryRecraftModifier(IEquipItem item, string modifierInstanceId, IReadOnlyCollection<string>? additiveResourceIds = null);

        /// <summary>Additives raise the success chance and may grant a second level on success.</summary>
        ItemUpgradeResult TryUpgradeItem(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null);

        /// <summary>The success chance of the NEXT sharpening attempt: level curve × (1 + mastery bonus)
        /// + flux bonuses, clamped to 0..1 — the exact probability <see cref="TryUpgradeItem"/> rolls
        /// against with the same resources. Non-additive ids in the collection are ignored, so callers
        /// may pass the full operation cost. Purely informational (the UI chance bar) — no state changes.</summary>
        float GetUpgradeChance(IEquipItem item, IReadOnlyCollection<string>? additiveResourceIds = null);

        /// <summary>The bare level-curve chance of the next attempt — no mastery, no fluxes. The
        /// forecast shows it struck through next to <see cref="GetUpgradeChance"/>.</summary>
        float GetBaseUpgradeChance(IEquipItem item);
    }
}
