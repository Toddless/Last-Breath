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
        private const string Icons = "res://Data/Shared/Assets/Icons/";
        private const string Items = "res://Data/Shared/Assets/Items/";
        private const string Resources = "res://Data/Shared/Assets/Resources/";
        private const string UI = "res://Data/Shared/Assets/UI/";

        // Recipes share one icon (a scroll) regardless of the crafted result.
        public const string RecipeIcon = $"{Icons}Recipe_Scroll.png";

        public static string AbilityIcon(string abilityId) => $"{Icons}{abilityId}.png";

        public static string ItemIcon(string itemId) => $"{Items}{itemId}.png";

        public static string ResourceIcon(string resourceId) => $"{Resources}{resourceId}.png";

        public static string SlotBackground(Rarity rarity) => $"{UI}Slot_Background_{rarity}.png";
    }
}
