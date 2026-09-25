namespace LastBreath.Descriptors.Preview
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Localization;
    using Tooling.Localization;

    /// <summary>
    /// The game's translation backend over the .po files a tool has OPEN. The game reads them through
    /// Godot's TranslationServer; a tool has no engine to ask and, more to the point, has the files as
    /// they are being edited — so a wording retyped a keystroke ago is the wording the next preview
    /// reads.
    /// <para>A key nobody has written stays a key. That is what the game shows too, and a preview that
    /// quietly invented a word for it would hide the one thing this panel is watched for.</para>
    /// </summary>
    public sealed class PreviewWording : ILocalizationProvider
    {
        /// <summary>The locale a preview opens in, and the one a missing wording falls back to: the game
        /// reads it first, so it is where a translation is noticed to be missing rather than invented.</summary>
        public const string Fallback = LocalizedTexts.ReferenceLocale;

        private readonly LocalizedTexts? _texts;

        private string _locale = Fallback;

        /// <param name="texts">The locales the run has open, or null for a run that could not read
        /// them — every key then stands for itself, which is a readable preview and not a broken one.</param>
        public PreviewWording(LocalizedTexts? texts) => _texts = texts;

        public event Action? LocaleChanged;

        /// <summary>The locales a preview may be read in — the ones the run actually opened.</summary>
        public IReadOnlyList<string> Locales => _texts?.Locales ?? [Fallback];

        /// <summary>Which of them is being read. A locale the run did not open is taken as written and
        /// simply answers nothing, which leaves every key falling back the way an unwritten one does.</summary>
        public string Locale
        {
            get => _locale;
            set
            {
                if (string.Equals(_locale, value, StringComparison.Ordinal)) return;

                _locale = value;
                LocaleChanged?.Invoke();
            }
        }

        public string Translate(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return Written(_locale, key) ?? Written(Fallback, key) ?? key;
        }

        /// <summary>The plural forms as a tool can read them: the .po writes one entry per form and the
        /// count picks between the two msgids the game passes. Which form a language actually takes for
        /// a count is the engine's rule, and a preview answering it differently would be inventing one.</summary>
        public string TranslatePlural(string singularKey, string pluralKey, int count) =>
            Translate(count == 1 ? singularKey : pluralKey);

        /// <summary>What one locale says under a key, or null when it says nothing — a locale the run
        /// never opened, a key it does not hold, and a key it holds empty are one answer here: the next
        /// locale is asked, and failing that the key stands for itself.</summary>
        private string? Written(string locale, string key)
        {
            if (_texts is not { } texts || !texts.Locales.Contains(locale, StringComparer.Ordinal)) return null;

            return texts.Read(locale, key) is { Length: > 0 } text ? text : null;
        }
    }
}
