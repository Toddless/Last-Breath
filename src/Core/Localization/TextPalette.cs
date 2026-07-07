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
            [DamageType.Pure] = "#ffd75e",
            [DamageType.Physical] = "#d0d0d0",
            [DamageType.Fire] = "#ff7043",
            [DamageType.Cold] = "#6ec6ff",
            [DamageType.Lightning] = "#b39dff",
            [DamageType.Poison] = "#7ccb64",
            [DamageType.Burning] = "#ff9d45",
            [DamageType.Bleed] = "#e05555",
        };

        public static string DamageColor(DamageType type) => s_damageColors.GetValueOrDefault(type, "#ffffff");

        public static string ColorizeNumber(string text) => Colorize(text, Number);

        public static string Colorize(string text, string colorHex) => $"[color={colorHex}]{text}[/color]";
    }
}
