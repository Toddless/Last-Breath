namespace Core.Crafting
{
    using Enums;
    using Items;

    /// <summary>Visibility rules for the crafting buttons on an item tooltip. The tooltip mirrors
    /// only the OBVIOUS surface (sealed items are immutable; only legendaries ascend) — the deep
    /// validation (mastery gate, full sharpening, resources) belongs to the crafting window and
    /// its request handlers, the single gates of the operations.</summary>
    public static class EquipItemCraftActions
    {
        // Data-born mythics ship pre-sharpened to their cap (UpdateLevel == MaxUpdateLevel, not sealed):
        // the cap check hides the button on them without a rarity special case.
        public static bool CanShowUpgrade(IEquipItem item) => !item.IsSealed && item.UpdateLevel < item.MaxUpdateLevel;

        // Nothing to reroll on an item without lines (data-born mythics like All-Cutting carry none).
        public static bool CanShowRecraft(IEquipItem item) =>
            !item.IsSealed && (item.Modifiers.Count > 0 || item.ContextModifiers.Count > 0);

        public static bool CanShowAscend(IEquipItem item) => !item.IsSealed && item.Rarity == Rarity.Legendary;
    }
}
