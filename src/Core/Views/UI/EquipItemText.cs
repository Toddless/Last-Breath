namespace Core.Views.UI
{
    using Enums;
    using Localization;

    /// <summary>Shared wording of an equip item's header parts: the weapon subtitle names the actual
    /// weapon (type + grip) next to the given rarity text — the item tooltip and the crafting bench
    /// print the same line through here.</summary>
    public static class EquipItemText
    {
        /// <summary>Prefix of the localization key wording a weapon type.</summary>
        public const string WeaponTypeKeyPrefix = "WeaponType_";

        /// <summary>Prefix of the localization key wording a grip.</summary>
        public const string HandednessKeyPrefix = "Handedness_";

        /// <summary>What two readings sharing one line of an item's header are separated by. Public
        /// because the subtitle is written in two halves — a weapon's here, everything else's beside it —
        /// and two spellings of one separator is two subtitles that do not line up.</summary>
        public const string Separator = " · ";

        /// <summary>The weapon subtitle: the already-worded rarity, then the weapon's type and grip.</summary>
        public static string WeaponSubtitle(string rarityText, WeaponType type, Handedness handedness) =>
            string.Join(Separator, rarityText,
                Localization.Localize(WeaponTypeKeyPrefix + type),
                Localization.Localize(HandednessKeyPrefix + handedness));
    }
}
