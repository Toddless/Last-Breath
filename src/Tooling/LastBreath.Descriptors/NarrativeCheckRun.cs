namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.DialogueData;
    using Core.Data.GameData;
    using Core.Data.NpcData;
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

        private const string NullListFormat = "{0} writes its {1} as null, so the run read no record out of it";

        private const string DialogueWord = "dialogues";

        private const string QuestWord = "quests";

        private const string NpcWord = "npcs";

        private const string FoundFormat = "narrative: {0} finding(s) — see checks";

        private const string DroppedFormat = "narrative: {0} finding(s), {1} record(s) dropped — see checks";

        private const string CleanText = "narrative: no findings";

        /// <summary>
        /// What a run has to say on the status line of a tool nobody pressed a button on: how much the
        /// narrative owes, and how much of it the loader is already throwing away. The dropped records are
        /// counted apart because they are the only sort that is not a warning — a dropped record is out of
        /// the game this second, and an author editing a quest the loader refuses is editing nothing.
        /// <para>Nothing at all for a run that found nothing: a clean narrative is what the author expects,
        /// and a line saying so at every reading is one he learns to read past.</para>
        /// </summary>
        public static string? Said(IReadOnlyList<NarrativeFinding> findings)
        {
            ArgumentNullException.ThrowIfNull(findings);

            if (findings.Count == 0) return null;

            int dropped = findings.Count(finding => finding.Kind == NarrativeFindingKind.Dropped);

            return dropped == 0
                ? Text(FoundFormat, findings.Count)
                : Text(DroppedFormat, findings.Count, dropped);
        }

        /// <summary>
        /// What a reading has to say when one before it has already spoken: the verdict, when it is not the
        /// one already standing, and the line saying the narrative owes nothing when it has just stopped
        /// owing — an author learns that he fixed the last quest where he learnt that he had broken it.
        /// <para>Nothing at all while the answer has not moved. A reading is made again after every step
        /// the tool files, and the same verdict said per keystroke writes over what the author's own
        /// gesture had to say.</para>
        /// </summary>
        public static string? Changed(string? last, string? now)
        {
            if (string.Equals(last, now, StringComparison.Ordinal)) return null;

            return now ?? CleanText;
        }

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

        /// <summary>The narrative as the rules are asked about it: the records as written — the npcs among
        /// them, since what a species claims it can do is half of a conversation — the records the loader
        /// kept, and the two sources answering for everything outside.</summary>
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
                Npcs = Written<NpcsData, NpcData>(workspace, DataCatalog.Npc, NpcWord, data => data.Npcs, notes),
                LoadedDialogues = Loaded(sandbox.Dialogues.All.Select(dialogue => dialogue.NpcId)),
                LoadedQuests = Loaded(sandbox.QuestCatalog.All.Select(quest => quest.Id)),
                Ids = new IndexedIds(references),
                Texts = Wording(texts)
            };
        }

        /// <summary>Every record one catalog WRITES, whether or not the loader kept it. A document that is
        /// not the shape the catalog expects is a note: the rest of the run reads on, the way the sandbox
        /// does with the same files. A file writing its list as null is one such shape — json takes the
        /// word and the reader is handed nothing, which is no records rather than a run that stops.</summary>
        private static IReadOnlyList<TRecord> Written<TData, TRecord>(
            CatalogWorkspace workspace,
            string catalog,
            string word,
            Func<TData, IEnumerable<TRecord>> records,
            ICollection<string> notes)
        {
            List<TRecord> written = [];

            foreach (GameDataFile file in WorkspaceDocuments.Open(workspace, catalog, notes))
            {
                try
                {
                    if (JsonConvert.DeserializeObject<TData>(file.Json) is not { } data) continue;

                    IEnumerable<TRecord>? list = records(data);

                    if (list is null) notes.Add(Text(NullListFormat, file.FileName, word));

                    written.AddRange(list ?? []);
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
