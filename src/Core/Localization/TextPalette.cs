namespace Core.Localization
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// The one place BBCode colors come from. UI and formatters reference these instead of
    /// inlining hex codes.
    /// </summary>
    public static class TextPalette
    {
        /// <summary>Numbers inside descriptions (damage, stacks, percentages).</summary>
        public const string Number = "#ffd75e";

        /// <summary>Clickable keywords in descriptions ({@Effect_X} links).</summary>
        public const string Keyword = "#8fd4e8";

        // Buff/debuff tint: over-head bar counters and effect tooltip titles.
        public const string Buff = "#8cbf73";
        public const string Debuff = "#d9735a";

        /// <summary>Weapon base-stat values (item tooltip, crafting bench preview).</summary>
        public const string BaseStat = "#c9a861";

        // Battle log accents
        public const string Crit = "#ffd75e";
        public const string System = "#909090";
        public const string AbilityName = "#8fd4e8";
        public const string EffectName = "#c9a0e8";
        public const string Heal = "#7ccb64";
        public const string Death = "#e05555";
        public const string Muted = "#b8b8b8";

        private static readonly Dictionary<DamageType, string> s_damageColors = new()
        {
            [DamageType.Sacred] = "#F5B64C",
            [DamageType.Physical] = "#B5AEAE",
            [DamageType.Fire] = "#E3562B",
            [DamageType.Cold] = "#43A4E5",
            [DamageType.Lightning] = "#8D6FF7",
            [DamageType.Poison] = "#55D458",
            [DamageType.Burning] = "#E35924",
            [DamageType.Bleed] = "#E82E2E",
            [DamageType.Blight] = "#7A4B78",
        };

        private static readonly Dictionary<Rarity, string> s_rarityColors = new()
        {
            [Rarity.Common] = "#d0d0d0",
            [Rarity.Uncommon] = "#7ccb64",
            [Rarity.Rare] = "#6ec6ff",
            [Rarity.Epic] = "#8845BF",
            [Rarity.Legendary] = "#E86A3F",
            [Rarity.Unique] = "#DE791D",
            [Rarity.Mythic] = "#F056D6",
        };

        /// <summary>Battle-log tint of a damage component. White is the deliberate fallback: it is what
        /// plain Physical would read as, so an unlisted type shows up as an untinted number rather than
        /// borrowing another type's colour.</summary>
        public static string DamageColor(DamageType type) => s_damageColors.GetValueOrDefault(type, "#ffffff");

        /// <summary>Item rarity accent: tooltip titles, slot frames and anything else that color-codes rarity.</summary>
        public static string RarityColor(Rarity rarity) => s_rarityColors.GetValueOrDefault(rarity, "#ffffff");

        public static string ColorizeNumber(string text) => Colorize(text, Number);

        public static string Colorize(string text, string colorHex) => $"[color={colorHex}]{text}[/color]";
    }
}
