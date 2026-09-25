namespace Tooling.Schema.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>One word a source offers under a field, and whether it stands for a whole family of them
    /// rather than for itself — a template written with its parameter named, which is picked to be filled
    /// in and never written as it stands.</summary>
    public readonly record struct SuggestedWord(string Word, bool Family);

    /// <summary>What the run has to say about the word standing in a field answered from an open list.</summary>
    public enum SuggestedWordKind
    {
        /// <summary>A word a source of this run has met, or nothing written at all.</summary>
        Met,

        /// <summary>A family's template with its parameter still named: the word it stands for was never
        /// written in, so nothing answers the key as it is spelt.</summary>
        Unfilled,

        /// <summary>A word nobody has met. Never a broken one: the list is open, and a field like this is
        /// where the first use of a word is written.</summary>
        New
    }

    /// <summary>
    /// How a word written into a suggested field is read against the words the run offers. Here rather
    /// than in the panel drawing it: which words count as met is the same question the game asks of a key
    /// it looks up, and a judgement living only inside a window is one nothing can be held to.
    /// </summary>
    public static class SuggestedWords
    {
        /// <summary>How a source writes the parameter of a family inside a template. A convention of the
        /// lists themselves and not of any one of them: a word carrying either mark is one still waiting
        /// for what it stands for.</summary>
        private const char ParameterOpen = '<';

        private const char ParameterClose = '>';

        /// <summary>
        /// The words a source offers, as one set to read a written word against. Told apart BY THE LETTER:
        /// capitals are how a search is forgiving and never how two words are one, and a word the game
        /// would look up as another must be marked as one nobody has met.
        /// </summary>
        public static IReadOnlySet<string> Met(IEnumerable<SuggestedWord> offered)
        {
            ArgumentNullException.ThrowIfNull(offered);

            return offered.Select(word => word.Word).ToHashSet(StringComparer.Ordinal);
        }

        /// <summary>What to say about the word standing in the box. Nothing written is nothing to judge:
        /// an empty field is the author having not started, which the schema already answers for.</summary>
        public static SuggestedWordKind Judge(string written, IReadOnlySet<string> met)
        {
            ArgumentNullException.ThrowIfNull(written);
            ArgumentNullException.ThrowIfNull(met);

            if (written.Length == 0) return SuggestedWordKind.Met;
            if (Unfilled(written)) return SuggestedWordKind.Unfilled;

            return met.Contains(written) ? SuggestedWordKind.Met : SuggestedWordKind.New;
        }

        /// <summary>Whether a word still carries the parameter of the family it is spelled after — a
        /// template taken out of the list and written down as it stood.</summary>
        public static bool Unfilled(string word)
        {
            ArgumentNullException.ThrowIfNull(word);

            return word.Contains(ParameterOpen) || word.Contains(ParameterClose);
        }
    }
}
