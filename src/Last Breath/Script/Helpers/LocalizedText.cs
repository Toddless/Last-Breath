namespace Playground.Script.Helpers
{
    using System.Collections.Generic;
    public class LocalizedText
    {
        private readonly Dictionary<string, string> _translations = [];

        public string GetText(string language = "default") => _translations.TryGetValue(language, out var text) ? text : string.Empty;
    }
}
