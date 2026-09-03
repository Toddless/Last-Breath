namespace Core.Narrative.Validation
{
    using System.Collections.Generic;
    using Data.DialogueData;
    using Data.QuestData;

    /// <summary>
    /// What one run of the cross-checks is asked about: the narrative as it is WRITTEN, which of it the
    /// game's own loader kept, and the two sources that answer for everything outside it.
    /// <para>The records are the raw ones and not the parsed definitions on purpose. A loader that
    /// refuses a route drops the whole record, so by the time there is a definition the thing to report
    /// is gone — and naming the node that dangles is the only report an author can act on.</para>
    /// </summary>
    public sealed record NarrativeCheckInput
    {
        public required IReadOnlyList<DialogueEntry> Dialogues { get; init; }

        public required IReadOnlyList<QuestEntry> Quests { get; init; }

        /// <summary>Npc ids whose dialogue the game's own loader kept. What is written and not here was
        /// refused whole, which is the loudest thing the run has to say about it.</summary>
        public required IReadOnlyCollection<string> LoadedDialogues { get; init; }

        public required IReadOnlyCollection<string> LoadedQuests { get; init; }

        public required INarrativeIdSource Ids { get; init; }

        public required INarrativeTextSource Texts { get; init; }
    }
}
