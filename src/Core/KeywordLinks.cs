namespace Core
{
    using Localization;
    using Views.UI;
    using Godot;

    /// <summary>
    /// Wires a RichTextLabel with description text to the keyword tooltip window: a click on a
    /// {@Key} link opens the reference card next to the cursor. Attach once per label (any UI
    /// showing rendered descriptions); the tooltip's own text is deliberately not attached.
    /// </summary>
    public static class KeywordLinks
    {
        public static void Attach(RichTextLabel? label)
        {
            if (label == null) return;
            label.BbcodeEnabled = true;
            label.MetaClicked += meta => Open(meta.ToString(), label);
        }

        private static void Open(string key, Control source)
        {
            if (key.Length == 0) return;
            var services = Services.GameServiceProvider.Instance;
            if (!services.GetService<IKeywordProvider>().TryGetTooltip(key, out var view)) return;

            services.GetService<IUiElementsManager>().ShowKeyword(view, source.GetGlobalMousePosition());
        }
    }
}
