namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.DialogueData;
    using Core.Data.GameData;
    using Core.Data.QuestData;
    using Core.Narrative;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Facts;
    using Core.Narrative.Quests;
    using Core.Narrative.Validation;
    using LastBreath.Descriptors.Sandbox;
    using Newtonsoft.Json;
    using Tooling.Catalogs;
    using Tooling.Localization;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>What one run of the checks came to: the narrative it was run over, what it found, the fact
    /// keys it met on the way, and what it could not read. The notes are that and nothing else — a catalog
    /// missing, a document of the wrong shape — so that a clean report means the run read everything, and
    /// not that a sandbox has said again what it is silent about by nature.</summary>
    public sealed record NarrativeCheckReport(
        NarrativeCheckInput Read,
        IReadOnlyList<NarrativeFinding> Findings,
        IReadOnlyList<string> Notes,
        FactKeyRegistry Facts);

    /// <summary>
    /// The game's narrative cross-checks over the documents a tool has open. The rules are the game's own
    /// (<see cref="NarrativeChecks"/>); this only answers the three questions they ask of the outside —
    /// which records the loader kept, which ids the run knows, and what the locales say — out of the
    /// workspace instead of out of a running game.
    /// </summary>
    public static class NarrativeCheckRun
    {
        private const string UnreadableFormat = "{0} could not be read as a {1} document: {2}";

        private const string DialogueWord = "dialogues";

        private const string QuestWord = "quests";

        /// <summary>Runs every rule over the documents as they are written this second. The records are
        /// read twice on purpose: once raw, because a route that dangles is only nameable before the
        /// loader drops the record it is in, and once through the game's own loader, which is the only
        /// thing that can say whether a record survived at all.</summary>
        public static NarrativeCheckReport Over(CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts)
        {
            List<string> notes = [];
            NarrativeCheckInput input = Narrative(workspace, references, texts, notes);
            NarrativeReading reading = NarrativeChecks.Read(input);

            return new NarrativeCheckReport(input, reading.Findings, notes, reading.Facts);
        }

        /// <summary>The narrative as the rules are asked about it: the records as written, the records the
        /// loader kept, and the two sources answering for everything outside.</summary>
        private static NarrativeCheckInput Narrative(
            CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts, ICollection<string> notes)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentNullException.ThrowIfNull(references);
            ArgumentNullException.ThrowIfNull(notes);

            NarrativeSandbox sandbox = NarrativeSandbox.Load(workspace, new SandboxWorldState());
            foreach (string note in sandbox.Notes) notes.Add(note);

            return new NarrativeCheckInput
            {
                Dialogues = Written<DialoguesData, DialogueEntry>(workspace, DataCatalog.Dialogues, DialogueWord, data => data.Dialogues, notes),
                Quests = Written<QuestsData, QuestEntry>(workspace, DataCatalog.Quests, QuestWord, data => data.Quests, notes),
                LoadedDialogues = Loaded(sandbox.Dialogues.All.Select(dialogue => dialogue.NpcId)),
                LoadedQuests = Loaded(sandbox.QuestCatalog.All.Select(quest => quest.Id)),
                Ids = new IndexedIds(references),
                Texts = Wording(texts)
            };
        }

        /// <summary>Every record one catalog WRITES, whether or not the loader kept it. A document that is
        /// not the shape the catalog expects is a note: the rest of the run reads on, the way the sandbox
        /// does with the same files.</summary>
        private static IReadOnlyList<TRecord> Written<TData, TRecord>(
            CatalogWorkspace workspace,
            string catalog,
            string word,
            Func<TData, IEnumerable<TRecord>> records,
            ICollection<string> notes)
        {
            List<TRecord> written = [];

            foreach (GameDataFile file in NarrativeDocuments.Open(workspace, catalog, notes))
            {
                try
                {
                    if (JsonConvert.DeserializeObject<TData>(file.Json) is { } data) written.AddRange(records(data));
                }
                catch (JsonException failure)
                {
                    notes.Add(Text(UnreadableFormat, file.FileName, word, failure.Message));
                }
            }

            return written;
        }

        private static HashSet<string> Loaded(IEnumerable<string> ids) => [.. ids];

        /// <summary>The locales of the run, or none at all when the folder holding them could not be read
        /// — which the rules say out loud rather than passing over every line unchecked.</summary>
        private static INarrativeTextSource Wording(LocalizedTexts? texts) =>
            texts is null
                ? new NarrativeTextSource(LocalizedTexts.ReferenceLocale, [], (_, _) => false)
                : new NarrativeTextSource(LocalizedTexts.ReferenceLocale, texts.Locales, (locale, key) => texts.Read(locale, key) is not null);

        /// <summary>The ids of the run as the game's targets ask for them. The two vocabularies are
        /// separate types on purpose — the game names no tool type — so the crossing is spelled out here,
        /// where the tool already reads the game's schemas.</summary>
        private sealed class IndexedIds(ReferenceIndex references) : INarrativeIdSource
        {
            public bool Describes(NarrativeReferenceTarget target) => references.Undescribed([Named(target)]).Count == 0;

            public bool Knows(NarrativeReferenceTarget target, string id) => references.Exists([Named(target)], id);

            private static ReferenceTarget Named(NarrativeReferenceTarget target) =>
                target.Section is null ? ReferenceTarget.Whole(target.Catalog) : new ReferenceTarget(target.Catalog, target.Section);
        }
    }
}
