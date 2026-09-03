namespace Core.Narrative.Validation
{
    using System;
    using System.Collections.Generic;

    /// <summary>Which localization keys a run knows, locale by locale. The rules say which keys the
    /// narrative writes; reading a .po file is nobody's business here — the game asks the engine's
    /// translations, an authoring tool asks the documents it has open.</summary>
    public interface INarrativeTextSource
    {
        /// <summary>The locale a key has to exist in. The others are coverage: a key the reference locale
        /// has and another one does not is a translation owed, not a line nobody wrote.</summary>
        string ReferenceLocale { get; }

        /// <summary>Every locale this run read, the reference one among them. Empty means the run read no
        /// wording at all, which the checks say once rather than pass over.</summary>
        IReadOnlyList<string> Locales { get; }

        /// <summary>Whether the locale writes anything under the key. A key written with no text is still
        /// a key: an untranslated entry shows the key in the game, and that is a translation owed rather
        /// than a line missing.</summary>
        bool Has(string locale, string key);
    }

    /// <summary>A text source made of the question itself, for a caller who has the answer but no type to
    /// hang it on.</summary>
    public sealed class NarrativeTextSource(
        string referenceLocale,
        IReadOnlyList<string> locales,
        Func<string, string, bool> has) : INarrativeTextSource
    {
        public string ReferenceLocale => referenceLocale;

        public IReadOnlyList<string> Locales => locales;

        public bool Has(string locale, string key) => has(locale, key);
    }
}
