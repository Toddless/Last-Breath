namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.GameData;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Json;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>What a walk over conversations came to: how many of them were written into, and the keys
    /// of all of them as one answer — a rename reaching several files is one gesture and reads as one
    /// line.</summary>
    public readonly record struct DialogueKeysFollowed(int Conversations, DialogueKeyResult Keys);

    /// <summary>
    /// The gesture both authoring tools make of <see cref="DialogueKeyPlan"/>: conversations settled and
    /// written as ONE step of the history, filed with the edit that caused it.
    /// <para>Two tools ask it for two reasons and the walk is the same. The narrative editor asks about
    /// the conversation on screen, whose node or line the author has just retyped; the data editor asks
    /// about every conversation of the run, because an npc renamed there words the keys of every line the
    /// npc speaks and those live in another catalog's files.</para>
    /// </summary>
    public static class DialogueKeyFollow
    {
        /// <summary>What the keys following the structure is called where the stack has nothing to name
        /// the step by: the pass is one gesture with the edit that moved them, and that edit's own name is
        /// what an author looks for.</summary>
        private const string StepText = "the wording follows the conversation";

        private const string WrittenFormat = "worded {0} key(s), moved {1}";

        /// <summary>What a pass that left places standing says: what it did get done, and every name that
        /// stopped one — a refusal naming the first of them reads as the only one.</summary>
        private const string NotMovedFormat = "{0}; {1} place(s) left as they stand — {2} already written";

        /// <summary>What a pass that could not put a wording back says. Loud on purpose and never left to
        /// the count above: the text is under a name nothing reads, and only the author knows which of the
        /// two texts fighting over the key is which.</summary>
        private const string ParkedFormat = "{0}; {1} wording(s) left parked and read by nothing — {2}";

        private const string KeyNameFormat = "“{0}”";

        private const string KeyNamesSeparator = ", ";

        /// <summary>A walk that had nothing to walk. Named rather than defaulted so that whoever tells the
        /// author about it is reading empty lists and not null ones.</summary>
        private static readonly DialogueKeysFollowed s_nothing = new(0, new DialogueKeyResult(0, 0, [], []));

        /// <summary>
        /// Brings every conversation of the run back to the keys its structure words — what an npc renamed
        /// in another catalog leaves behind, since a line is read under the name of whoever speaks it.
        /// <para>Nothing at all where the run never opened the conversations: the keys would be moved in
        /// files this tool cannot save.</para>
        /// </summary>
        public static DialogueKeysFollowed All(
            CatalogWorkspace workspace, LocalizedTexts? texts, EditHistory history, object owner)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            CatalogView? dialogues = workspace.Catalogs.FirstOrDefault(
                view => string.Equals(view.Catalog, DataCatalog.Dialogues, StringComparison.Ordinal));

            return dialogues is { } view ? Over(view.Records, texts, history, owner) : s_nothing;
        }

        /// <summary>
        /// Settles the conversations and writes them: a line just added is given the key it will be read
        /// under, and a node, an option or the npc himself renamed carries the wording in every locale
        /// along with the name.
        /// <para>Nothing is done without the locales: the keys would be written into the files alone and
        /// every line would be left pointing at a text no locale holds, which is the one state this whole
        /// gesture exists to prevent.</para>
        /// <para>Settled whole before a step is opened, and one step for all of them: a pass may refuse
        /// every place it was asked about, and a step opened for one would take the author's own edit into
        /// a group of the tool's making and leave files called changed by a gesture that wrote nothing.
        /// <paramref name="owner"/> is the document the edit that caused this landed in — the step is
        /// filed with that one, because the keys move because of it.</para>
        /// </summary>
        public static DialogueKeysFollowed Over(
            IReadOnlyList<CatalogRecord> conversations, LocalizedTexts? texts, EditHistory history, object owner)
        {
            ArgumentNullException.ThrowIfNull(conversations);
            ArgumentNullException.ThrowIfNull(history);
            ArgumentNullException.ThrowIfNull(owner);

            if (texts is not { } locales) return s_nothing;

            List<Planned> planned = Settled(conversations, locales);

            if (planned.Count == 0) return s_nothing;

            // Named after the edit that caused it, which is the one this step takes in: the keys move
            // because a node was renamed or a line added, and an author reading back what he can undo is
            // looking for the gesture he made and not for what the tool did about it.
            IDisposable? step = planned.Any(one => one.Pass.Writes)
                ? history.GroupWithNewest(history.NextUndo ?? StepText, owner)
                : null;

            try
            {
                return Written(planned, locales);
            }
            finally
            {
                step?.Dispose();
            }
        }

        /// <summary>What a pass over the wording is told as: what it wrote and moved, then the places it
        /// left standing and the names that stopped them, then any wording it could not put back. Every
        /// half and not one — a pass that moved four keys and refused a fifth did something, and a line
        /// saying only the refusal reads as if the gesture had done nothing at all.
        /// <para>Nothing at all for a pass with nothing to say: a gesture ends every time a box is left,
        /// and most of them leave every key exactly where it was.</para></summary>
        public static string? Said(DialogueKeyResult result)
        {
            if (result.Written + result.Moved + Count(result.Taken) + Count(result.Parked) == 0) return null;

            string done = Text(WrittenFormat, result.Written, result.Moved);

            if (Count(result.Taken) > 0) done = Text(NotMovedFormat, done, result.Taken.Count, Names(result.Taken));

            if (Count(result.Parked) == 0) return done;

            return Text(ParkedFormat, done, result.Parked.Count, Names(result.Parked));
        }

        /// <summary>What each conversation would be written as, asked of all of them before any of them is
        /// written. A conversation whose keys already answer to its structure is not in the list: it has
        /// no pass, and a step opened for it would file an empty gesture.</summary>
        private static List<Planned> Settled(IReadOnlyList<CatalogRecord> conversations, LocalizedTexts texts)
        {
            List<Planned> planned = [];

            foreach (CatalogRecord record in conversations)
            {
                JsonTreeDocument document = record.File.Document;
                IReadOnlyList<DialogueTextPlace> off = DialogueKeyPlan.OffPattern(document, record.Pointer);

                if (off.Count == 0) continue;

                planned.Add(new Planned(document, DialogueKeyPlan.Plan(off, texts)));
            }

            return planned;
        }

        /// <summary>Writes every settled pass and folds what they came to into one answer. A conversation
        /// counts as followed when something of it actually moved — a pass refused whole changed no file,
        /// and counting it would tell the author that a conversation followed his rename when it did
        /// not.</summary>
        private static DialogueKeysFollowed Written(IReadOnlyList<Planned> planned, LocalizedTexts texts)
        {
            int conversations = 0;
            int written = 0;
            int moved = 0;
            List<string> taken = [];
            List<string> parked = [];

            foreach ((JsonTreeDocument document, DialogueKeyPass pass) in planned)
            {
                DialogueKeyResult one = DialogueKeyPlan.Apply(document, pass, texts);

                if (one.Written + one.Moved > 0) conversations++;

                written += one.Written;
                moved += one.Moved;
                taken.AddRange(one.Taken);
                parked.AddRange(one.Parked);
            }

            return new DialogueKeysFollowed(conversations, new DialogueKeyResult(written, moved, taken, parked));
        }

        private static int Count(IReadOnlyList<string>? names) => names?.Count ?? 0;

        private static string Names(IReadOnlyList<string> names) =>
            string.Join(KeyNamesSeparator, names.Select(name => Text(KeyNameFormat, name)));

        /// <summary>One conversation as it will be written, held between the settling and the writing so
        /// that the whole run is settled first.</summary>
        private readonly record struct Planned(JsonTreeDocument Document, DialogueKeyPass Pass);
    }
}
