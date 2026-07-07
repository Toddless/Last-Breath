namespace Core.Localization
{
    using System;

    /// <summary>
    /// The translation backend. Pure interface: the game talks to this, Godot's TranslationServer
    /// hides behind the adapter, tests use a dictionary fake.
    /// </summary>
    public interface ILocalizationProvider
    {
        /// <summary>Raised after the locale changes at runtime — open UI must rebuild its texts.</summary>
        event Action? LocaleChanged;

        string Locale { get; set; }

        string Translate(string key);

        /// <summary>Gettext plural: keys are the English singular/plural msgids; the .po picks the form for the count.</summary>
        string TranslatePlural(string singularKey, string pluralKey, int count);
    }
}
