namespace Core.Constants
{
    using Enums;

    /// <summary>
    /// Canonical res:// roots of the shared asset tree — the single physical copy lives in
    /// SharedData/Assets and reaches each project through the Data/Shared symlink.
    /// NOTE: Crafting mounts the symlink at Internal/Data/Shared — prefix accordingly there.
    /// </summary>
    public static class AssetPaths
    {
        public const string Icons = "res://Data/Shared/Assets/Icons/";
        public const string Items = "res://Data/Shared/Assets/Items/";
        public const string Resources = "res://Data/Shared/Assets/Resources/";
        public const string UI = "res://Data/Shared/Assets/UI/";

        // Recipes share one icon (a scroll) regardless of the crafted result.
        public const string RecipeIcon = $"{Icons}Recipe_Scroll.png";

        // Inventory-slot chrome: one background per rarity plus a single neutral frame overlay.
        public const string SlotFrame = $"{UI}Slot_Frame.png";

        public static string AbilityIcon(string abilityId) => $"{Icons}{abilityId}.png";

        public static string ItemIcon(string itemId) => $"{Items}{itemId}.png";

        public static string ResourceIcon(string resourceId) => $"{Resources}{resourceId}.png";

        public static string SlotBackground(Rarity rarity) => $"{UI}Slot_Background_{rarity}.png";
    }
}
