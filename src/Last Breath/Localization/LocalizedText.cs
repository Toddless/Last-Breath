namespace Playground.Localization
{
    using System.Collections.Generic;
    public class LocalizedText
    {
        private readonly Dictionary<string, Dictionary<string, string>> _translations = [];

        public void AddTranslation(string language, Dictionary<string, string> text)
        {
            _translations[language] = text;
        }

        public void LoadTranslations(Dictionary<string, Dictionary<string, string>> translations)
        {
            foreach (var pair in translations)
            {
                AddTranslation(pair.Key, pair.Value);
            }
        }

        public Dictionary<string, string> GetText(string key) => _translations[key]; 
    }
}
