namespace Core.Localization
{
    using System;
    using Enums;

    /// <summary>
    /// The damage-type keyword family: a <c>{@Fire}</c> / <c>{@Bleed}</c> marker in a description
    /// spells the enum member's own name (the battle log's <c>DamageType_</c> prefix is tolerated
    /// so a log-style key colors the same). One parse shared by the template engine (link tint)
    /// and the keyword provider (card title tint), so a type can never be colored in the text and
    /// plain on its card.
    /// </summary>
    public static class DamageKeywords
    {
        private const string LogPrefix = "DamageType_";

        /// <summary>The canonical keyword key of a type — what a <c>{@...}</c> marker spells and
        /// what the .po words the card under (<c>&lt;Key&gt;</c> / <c>&lt;Key&gt;_Tooltip</c>).</summary>
        public static string Key(DamageType type) => type.ToString();

        public static bool TryParse(string key, out DamageType type)
        {
            string name = key.StartsWith(LogPrefix, StringComparison.Ordinal) ? key[LogPrefix.Length..] : key;
            // Enum.TryParse alone would also accept numeric spellings ("16"); a keyword is always a name.
            if (name.Length > 0 && !char.IsAsciiDigit(name[0]) && Enum.TryParse(name, ignoreCase: false, out type))
                return true;

            type = default;
            return false;
        }
    }
}
