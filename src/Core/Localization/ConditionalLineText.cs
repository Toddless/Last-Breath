namespace Core.Localization
{
    /// <summary>How a line held up by a predicate reads. A condition is worded once per catalog id
    /// (<c>Condition_&lt;id&gt;</c>) and the <c>Modifier_Conditional</c> template joins that clause to the
    /// line it gates, so every panel listing lines announces a gate the same way.
    /// <para>The wording is looked up by id and by nothing else: what a predicate is made of stays in the
    /// condition catalog, and a rule reshaped there keeps the words its author chose for it.</para></summary>
    public static class ConditionalLineText
    {
        private const string ClauseKeyPrefix = "Condition_";
        private const string TemplateKey = "Modifier_Conditional";
        private const string LinePlaceholder = "{line}";
        private const string ClausePlaceholder = "{condition}";

        /// <summary>The string-catalog key that words a condition id.</summary>
        public static string ClauseKey(string conditionId) => ClauseKeyPrefix + conditionId;

        /// <summary>The line as the player reads it: handed back untouched when nothing gates it, joined to
        /// its clause when something does. A gate the catalog has no wording for shows its key, the same miss
        /// any other string shows — a gated line never reaches the player looking unconditional.</summary>
        public static string Join(ILocalizationProvider localization, string line, string? conditionId, TextFormat format = TextFormat.Plain)
        {
            if (string.IsNullOrWhiteSpace(conditionId)) return line;

            string clause = localization.Translate(ClauseKey(conditionId));
            return localization.Translate(TemplateKey)
                .Replace(LinePlaceholder, line)
                .Replace(ClausePlaceholder, format == TextFormat.Rich ? TextPalette.Colorize(clause, TextPalette.Muted) : clause);
        }
    }
}
