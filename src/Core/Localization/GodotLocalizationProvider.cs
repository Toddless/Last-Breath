namespace Core.Localization
{
    using System;
    using Godot;

    /// <summary>The Godot adapter over TranslationServer (.po files from project settings).</summary>
    public class GodotLocalizationProvider : ILocalizationProvider
    {
        public event Action? LocaleChanged;

        public string Locale
        {
            get => TranslationServer.GetLocale();
            set
            {
                if (TranslationServer.GetLocale() == value) return;
                TranslationServer.SetLocale(value);
                LocaleChanged?.Invoke();
            }
        }

        public string Translate(string key) => TranslationServer.Translate(key);

        public string TranslatePlural(string singularKey, string pluralKey, int count) =>
            TranslationServer.TranslatePlural(singularKey, pluralKey, count);
    }
}
