namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.Schema;
    using Core.Narrative.Facts;
    using Tooling.Catalogs;
    using Tooling.Localization;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>
    /// The fact keys of a run, offered under the box one is written in: the families the game's own code
    /// keeps, written with their parameters named — <c>Npc_Talked:&lt;npcId&gt;</c> — and every word the
    /// dialogues and the quests already write or ask about.
    /// <para>An OPEN list. A key nobody has met is written and read exactly as any other, and the only
    /// thing said about it is that this run has not met it: the first use of a word is written in a field
    /// like the ones this answers. A family is offered as the family it is, so that the template picked
    /// out of the list is read as a word still waiting for its parameter and not as a key.</para>
    /// <para>Read on the first question and held until the documents move. The reading is the game's own
    /// narrative run over every open document, and asking it again per keystroke of a search field is a
    /// walk of the whole narrative per letter — so whoever hands this out says when a step has been filed,
    /// and the next question reads the documents as they stand then.</para>
    /// </summary>
    public sealed class FactKeySuggestions(CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts)
    {
        /// <summary>The name a schema names this list by. One word, so that the field saying where it is
        /// answered from and whoever answers it cannot drift apart.</summary>
        public const string Source = SuggestionSources.FactKeys;

        private const string RefusedFormat =
            "the fact keys could not be read over the documents as they stand, so none are offered: {0}";

        private readonly HashSet<string> _said = new(StringComparer.Ordinal);

        private FactKeyRegistry? _read;

        /// <summary>What a reading came to besides the keys: what the run could not read of the documents
        /// behind them, and the refusal of a run that threw. Said ONCE for each note, however many readings
        /// meet it: a workspace holding a document nothing can read complains at every step the tool files,
        /// and the same line on the status bar per gesture is noise the author reads past.</summary>
        public event Action<string>? Said;

        /// <summary>The words a query names, for a host handing this to an inspector. Nothing at all for
        /// any other source: a list answering about words it does not keep would offer fact keys under a
        /// field that has nothing to do with the world's facts.</summary>
        public IReadOnlyList<SuggestedWord> For(string source, string query) =>
            string.Equals(source, Source, StringComparison.Ordinal) ? Matching(query) : [];

        /// <summary>The keys a query names, families first and then the words the documents write. An empty
        /// query names them all — whoever asked does not remember the word, which is why he asked.</summary>
        public IReadOnlyList<SuggestedWord> Matching(string query) =>
            [.. NarrativeFactKeys.Matching(Read(), query).Select(key => new SuggestedWord(key.Key, key.Declared))];

        /// <summary>Forgets the reading. The words offered under a field are the ones the documents write
        /// NOW, and every step the tool files writes one: whoever holds the stack says a step was filed,
        /// and the next question walks the documents again.</summary>
        public void Invalidate() => _read = null;

        /// <summary>The reading, made on the first question and held. A run that throws is a document being
        /// typed into: the field goes on being a plain box, and the refusal is said rather than swallowed.</summary>
        private FactKeyRegistry Read()
        {
            if (_read is { } already) return already;

            try
            {
                FactKeyReading reading = NarrativeFactKeys.Over(workspace, references, texts);

                _read = reading.Keys;

                // What the run could not read is why a word is missing from the list under the box: said
                // out loud, because a shorter list looks exactly like a narrative nobody has written yet.
                foreach (string note in reading.Notes) Say(note);
            }
            catch (Exception failure)
            {
                _read = FactKeyRegistry.Over([]);
                Say(Text(RefusedFormat, failure.Message));
            }

            return _read;
        }

        /// <summary>Says a note the first time a reading meets it. Held for the life of this list and not
        /// dropped with the reading: what is wrong with a document survives the step that invalidated it,
        /// and the author has already been told.</summary>
        private void Say(string note)
        {
            if (_said.Add(note)) Said?.Invoke(note);
        }
    }
}
