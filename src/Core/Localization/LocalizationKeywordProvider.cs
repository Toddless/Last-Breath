namespace Core.Localization
{
    /// <summary>
    /// Keyword tooltips straight from the .po by convention: name = &lt;Key&gt;, text =
    /// &lt;Key&gt;_Tooltip (static reference wording) falling back to &lt;Key&gt;_Description.
    /// A key with no translations at all yields no tooltip — the link degrades gracefully.
    /// </summary>
    public class LocalizationKeywordProvider(ILocalizationProvider localization) : IKeywordProvider
    {
        public bool TryGetTooltip(string key, out KeywordTooltipView view)
        {
            string name = localization.Translate(key);
            string description = Translated(key + "_Tooltip") ?? Translated(key + "_Description") ?? string.Empty;
            view = new KeywordTooltipView(key, name, description);
            return name != key || description.Length > 0;
        }

        /// <summary>TranslationServer echoes the key back when there is no entry — treat that as "missing".</summary>
        private string? Translated(string key)
        {
            string text = localization.Translate(key);
            return text == key || text.Length == 0 ? null : text;
        }
    }
}
