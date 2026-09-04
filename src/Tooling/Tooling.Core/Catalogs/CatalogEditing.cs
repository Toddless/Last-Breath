namespace Tooling.Catalogs
{
    using System;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Localization;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>What an edit of a catalog's records came to: the record it left standing, and the reason
    /// it refused when it did. A refusal is a sentence and never an exception — everything an author can
    /// ask for wrongly (a name nobody typed, a name already taken, a catalog with nowhere to put a file)
    /// is something the tool has to be able to say back to him.</summary>
    public sealed record CatalogEditResult(CatalogRecord? Record, string? Note)
    {
        public bool Done => Note is null;
    }

    /// <summary>
    /// What a rename carried with it: how many places were rewritten, across how many files, and what the
    /// wording came to. <see cref="Refused"/> is the reason nothing at all was done — the two answers are
    /// held apart because they are two different things to tell the author, and only one of them means
    /// the record is still called what it was.
    /// </summary>
    /// <remarks>The keys are the wording's own answer (<see cref="LocalizedRename"/>), which says both how
    /// many moved and the name that stopped them when none did. That refusal is not this one: the id and
    /// its references have moved by then, and a locale that would not follow is a sentence to read rather
    /// than a rename to take back.</remarks>
    public sealed record CatalogRenameResult(int Uses, int Files, LocalizedRename Keys, string? Refused)
    {
        public bool Done => Refused is null;
    }

    /// <summary>
    /// Adding, copying and taking out whole records, by the rules the catalog's own schema states: the
    /// shape says how a record is written into its section, and the placement says which file it is
    /// written to. Godot-free like everything the host draws from, so the rules can be read and checked
    /// without a window.
    /// <para>Every change goes through the document, which puts it on the file's history: one press of
    /// undo takes a whole record back, and one press of redo puts it back where it stood.</para>
    /// </summary>
    public static class CatalogEditing
    {
        /// <summary>What a file laid down by the tool holds before anything is written into it. The
        /// section and the first record of it arrive together, as one step of the history.</summary>
        private const string EmptyDocument = "{}";

        /// <summary>
        /// Writes a fresh record of <paramref name="sectionKey"/> into the catalog, at the end of the
        /// section, in the file the catalog's placement rule names for it.
        /// <para><paramref name="fileChoice"/> is the file the caller picked, and it is the only say the
        /// caller has: a catalog split by a field of the record is split by that field here too — the
        /// choice is written into the record and the rule is asked again — so a record can never sit in a
        /// file that disagrees with what it says about itself.</para>
        /// </summary>
        public static CatalogEditResult AddRecord(CatalogView view, string? sectionKey, string? fileChoice, string? id)
        {
            ArgumentNullException.ThrowIfNull(view);

            if (view.Schema.Shape == RootShape.Single) return Refused(Notes.OneRecordOnly);
            if (Section(view, sectionKey) is not { } section) return Refused(Text(Notes.NoSection, sectionKey));

            string wanted = (id ?? string.Empty).Trim();

            if (Refusal(view, section.Key, section.Record, wanted) is { } refusal) return Refused(refusal);

            JObject blank = RecordTemplates.Blank(section.Record);

            if (section.Record.IdField is { } named && wanted.Length > 0) blank[named] = new JValue(wanted);

            // Written before the rule is asked: for a catalog split by a field of the record, the file
            // the author picked IS that field's value, and a record placed by a word it does not carry
            // would be found by the tool and never by the game.
            if (view.Schema.Placement is FieldFilePlacement placed && Named(fileChoice) is { } chosen)
                blank[placed.FieldName] = new JValue(chosen);

            if (Wanted(view, blank, fileChoice) is not { } name) return Refused(Notes.NoFileNamed);
            if (Held(view, name) is { } held) return Written(view, held, fresh: false, section, blank, wanted);
            if (Fresh(view, name, out string? nowhere) is not { } made) return Refused(nowhere!);

            return Written(view, made, fresh: true, section, blank, wanted);
        }

        /// <summary>Writes a copy of a record next to the one it was taken from, under a name of its own.
        /// The copy carries everything the original does, so an author writing a family of records edits
        /// what differs instead of laying every key down again.</summary>
        public static CatalogEditResult DuplicateRecord(CatalogView view, CatalogRecord record, string? id)
        {
            ArgumentNullException.ThrowIfNull(view);
            ArgumentNullException.ThrowIfNull(record);

            if (record.Token is not JObject token) return Refused(Notes.Gone);

            string wanted = (id ?? string.Empty).Trim();

            if (Refusal(view, record.Section, record.Schema, wanted) is { } refusal) return Refused(refusal);

            var copy = (JObject)token.DeepClone();

            if (record.Schema.IdField is { } named && wanted.Length > 0) copy[named] = new JValue(wanted);

            return Beside(view, record, copy, wanted);
        }

        /// <summary>Takes a record out of its file. The catalog is read again, so what stood below it
        /// carries the addresses it has now and not the ones it had a moment ago.</summary>
        public static CatalogEditResult RemoveRecord(CatalogView view, CatalogRecord record)
        {
            ArgumentNullException.ThrowIfNull(view);
            ArgumentNullException.ThrowIfNull(record);

            if (record.Token is null) return Refused(Notes.Gone);
            if (!record.File.Document.Remove(record.Pointer)) return Refused(Text(Notes.NotRemoved, record.CurrentId));

            view.Reread();

            return new CatalogEditResult(null, null);
        }

        /// <summary>The section a record is being written into: the one the caller named, or the only one
        /// the catalog has when it has one and the caller named nothing.</summary>
        public static SectionSchema? Section(CatalogView view, string? key)
        {
            ArgumentNullException.ThrowIfNull(view);

            if (key is null) return view.Schema.Sections.Count == 1 ? view.Schema.Sections[0] : null;

            foreach (SectionSchema section in view.Schema.Sections)
                if (string.Equals(section.Key, key, StringComparison.Ordinal))
                    return section;

            return null;
        }

        /// <summary>Whether the section already writes a record under this name, asked across every file
        /// at once: the game reads a section over the whole folder into one table.
        /// <para>Case is not part of the answer: two ids differing only in it are one word to the author,
        /// and a reference he writes by hand would land in whichever of the two he was not looking
        /// at.</para>
        /// <para><paramref name="except"/> is the record the question is asked on behalf of, where there
        /// is one: a record being renamed already carries the name being asked about, and a record does
        /// not take its own name.</para></summary>
        public static bool Taken(CatalogView view, string section, string id, CatalogRecord? except = null)
        {
            ArgumentNullException.ThrowIfNull(view);
            ArgumentNullException.ThrowIfNull(section);

            return view.Records.Any(record =>
                !Itself(record, except)
                && string.Equals(record.Section, section, StringComparison.Ordinal)
                && string.Equals(record.CurrentId, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Why the record may not be listed under the name typed into its id, or null when it may. The
        /// question a rename asks is the one an addition asks — a section is read into one table, and the
        /// second record of a name is the one the game drops — so it is answered by the same judge and
        /// said in the same words.
        /// <para>The record is never in its own way: the name is written into it as it is typed, and a
        /// record renamed to what it is already called, or to the same word in another case, has taken
        /// nothing from anybody. A name nobody typed is no rename at all — a record without one is listed
        /// under the place it stands in, which no other record can be spoken for.</para>
        /// </summary>
        public static string? RenameRefusal(CatalogView view, CatalogRecord record, string? typed)
        {
            ArgumentNullException.ThrowIfNull(view);
            ArgumentNullException.ThrowIfNull(record);

            string wanted = (typed ?? string.Empty).Trim();

            if (wanted.Length == 0) return null;

            return Taken(view, record.Section, wanted, record) ? TakenNote(record.Section, wanted) : null;
        }

        /// <summary>
        /// Carries a record's new name everywhere else the run writes the old one: every reference
        /// pointing at it, in every catalog and every file, and the localization keys its own catalog
        /// words from the id. One step of the history for all of it — a rename half taken back leaves a
        /// record read under one word by the files and under another by everything naming it, which is
        /// exactly what the author cannot see and cannot repair.
        /// <para>The id in the record's OWN file is not written here. It is written by whoever renamed the
        /// record — a box the author is typing in, a caller that set the field — and taken into this step
        /// by <see cref="Editing.History.EditHistory.GroupWithNewest"/>, so the word and everything that
        /// followed it are one thing to take back.</para>
        /// <para>Refused whole and doing nothing when the new name is already written in the section, and
        /// when a map already carries a key under it. A rename that stopped halfway through the files
        /// would be the tool making a state nobody asked for.</para>
        /// <para>A word nobody typed is no rename: an empty name either side, and a record renamed to what
        /// it is already called, move nothing and refuse nothing.</para>
        /// </summary>
        public static CatalogRenameResult RenameEverywhere(
            ReferenceUses uses,
            CatalogView view,
            CatalogRecord record,
            string oldId,
            string newId,
            LocalizedTexts? texts = null)
        {
            ArgumentNullException.ThrowIfNull(uses);
            ArgumentNullException.ThrowIfNull(view);
            ArgumentNullException.ThrowIfNull(record);

            string from = (oldId ?? string.Empty).Trim();
            string to = (newId ?? string.Empty).Trim();

            if (RenameRefusal(view, record, to) is { } refusal) return Nothing(refusal);
            if (from.Length == 0 || to.Length == 0 || string.Equals(from, to, StringComparison.Ordinal)) return Nothing(null);

            IReadOnlyList<ReferenceUse> written = uses.UsesOf(ReferenceUses.Naming(view, record), from);

            if (Blocked(written, to) is { } blocked) return Nothing(blocked);

            JsonTreeDocument owner = record.File.Document;
            HashSet<CatalogFile> files = [];
            int rewritten = 0;
            LocalizedRename keys = default;

            using (owner.History.GroupWithNewest(Text(Notes.RenameStep, from, to), owner))
            {
                foreach (ReferenceUse use in Writable(written))
                {
                    if (!Rewritten(use, to)) continue;

                    rewritten++;
                    files.Add(use.File);
                }

                if (texts is { } wording) keys = wording.RenameRecord(from, to, view.Schema.LocalizedSuffixes);
            }

            return new CatalogRenameResult(rewritten, files.Count, keys, null);
        }

        private static CatalogEditResult Refused(string note) => new(null, note);

        private static CatalogRenameResult Nothing(string? refused) => new(0, 0, default, refused);

        /// <summary>The uses in an order they can all be written in: the deepest first, so that whatever
        /// stands INSIDE a map keyed by the old word is written before the key itself moves and carries
        /// the addresses of everything under it away. A value written over keeps its address, so nothing
        /// else here depends on the order.</summary>
        private static IEnumerable<ReferenceUse> Writable(IReadOnlyList<ReferenceUse> uses) =>
            uses.OrderByDescending(use => use.At.Segments.Count);

        /// <summary>Writes the new word where one use of the old one stands. A key of a map is given
        /// another key rather than another value: the word IS the address there, and writing over it
        /// would leave the map keyed by the id nobody has any more.</summary>
        private static bool Rewritten(ReferenceUse use, string to) =>
            use.Kind == ReferenceUseKind.MapKey
                ? use.File.Document.RenameKey(use.At, to)
                : use.File.Document.SetValue(use.At, new JValue(to));

        /// <summary>The map that cannot take the new word, or null when every one of them can. A map
        /// already keyed by it would have one key swallow the other, and what stood under the one that
        /// went is a value no undo can name — so the whole rename is refused rather than made in part.</summary>
        private static string? Blocked(IReadOnlyList<ReferenceUse> uses, string to)
        {
            foreach (ReferenceUse use in uses)
            {
                if (use.Kind != ReferenceUseKind.MapKey || use.At.Parent is not { } map) continue;

                if (use.File.Document.Resolve(map) is JObject holder && holder.ContainsKey(to))
                    return Text(Notes.KeyTaken, to, use.File.Name);
            }

            return null;
        }

        /// <summary>Whether two records are the same one, asked by where it stands and not by what it
        /// holds: the id a record was listed under is exactly what a rename changes.</summary>
        private static bool Itself(CatalogRecord record, CatalogRecord? asking) =>
            asking is not null && ReferenceEquals(record.File, asking.File) && record.Pointer == asking.Pointer;

        /// <summary>That a name is spoken for, named where the catalog has a word for the section holding
        /// it: an author told a name is taken has to be able to go and look at the record holding it, and
        /// a catalog written as one nameless section shows him no section to look in.</summary>
        private static string TakenNote(string section, string id) =>
            section.Length > 0 ? Text(Notes.IdTakenIn, id, section) : Text(Notes.IdTaken, id);

        /// <summary>Why a name will not do, or null when it will. A record the shape or the schema names
        /// by its id has to have one, and no section may write the same name twice.</summary>
        private static string? Refusal(CatalogView view, string section, RecordSchema schema, string id)
        {
            if (CatalogRecords.Names(view.Schema, schema) && id.Length == 0) return Notes.NoId;
            if (id.Length == 0 || !Taken(view, section, id)) return null;

            return TakenNote(section, id);
        }

        private static string? Named(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        /// <summary>The file the record goes to: what the catalog's rule makes of the record itself, and
        /// the caller's choice where the rule has no answer. Null when neither side named one.</summary>
        private static string? Wanted(CatalogView view, JObject blank, string? fileChoice) =>
            view.Schema.Placement.FileFor(field => Field(blank, field)) ?? Named(fileChoice);

        /// <summary>One field of the record being placed, spelled the way the file spells it.</summary>
        private static string? Field(JObject blank, string jsonName) =>
            blank.TryGetValue(jsonName, StringComparison.Ordinal, out JToken? value) && value is JValue { Value: not null }
                ? JsonScalars.Written(value)
                : null;

        /// <summary>The file the catalog is already written under that name, or null when it has none.</summary>
        private static CatalogFile? Held(CatalogView view, string name)
        {
            foreach (CatalogFile file in view.Files)
                if (string.Equals(file.BaseName, name, StringComparison.OrdinalIgnoreCase))
                    return file;

            return null;
        }

        /// <summary>A file the catalog does not have yet, laid down in the catalog's own folder — and not
        /// taken into the run here: it joins the catalog once a record is actually written into it. Null
        /// with the reason written out when there is nowhere to lay one.</summary>
        private static CatalogFile? Fresh(CatalogView view, string name, out string? refusal)
        {
            refusal = null;

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                refusal = Text(Notes.UnusableName, name);
                return null;
            }

            // The catalog's own folder, whether or not anything in it was read: a folder that is there
            // and empty is exactly where an author writes his first record, and one that is not there is
            // a catalog this run never opened.
            if (!Directory.Exists(view.Folder))
            {
                refusal = Text(Notes.NoFolder, view.Folder, name);
                return null;
            }

            string path = Path.Combine(view.Folder, name + CatalogWorkspace.FileExtension);

            // A file on disk the run does not hold is one that would not read: writing a record into a
            // fresh document over it would drop everything it holds.
            if (File.Exists(path))
            {
                refusal = Text(Notes.Unreadable, Path.GetFileName(path));
                return null;
            }

            return new CatalogFile(path, JsonTreeDocument.Parse(EmptyDocument));
        }

        /// <summary>Puts the record at the end of its section, and the section itself into the file when
        /// the file does not carry it yet — both as one step, so one press of undo takes back the whole
        /// gesture and never half of it.</summary>
        private static CatalogEditResult Written(
            CatalogView view, CatalogFile file, bool fresh, SectionSchema section, JObject blank, string id)
        {
            JsonPointer at = CatalogRecords.Locate(section.Key);
            JsonTreeDocument document = file.Document;
            JToken? held = document.Resolve(at);
            bool map = view.Schema.Shape == RootShape.Dictionary;

            if (held is null)
                return Sectioned(view, file, fresh, document, section.Key,
                    map ? new JObject(new JProperty(id, blank)) : new JArray(blank),
                    map ? at.Append(id) : at.Append(0));

            if (map)
                return held is JObject
                    ? Placed(view, file, fresh, document.Insert(at, id, blank), at.Append(id))
                    : Refused(Text(Notes.NoRoom, file.Name));

            if (held is not JArray array) return Refused(Text(Notes.NoRoom, file.Name));

            // The place is read before the write and not after it: the section is one longer once the
            // record is in it, and an address worked out from that names the record's neighbour.
            int index = array.Count;

            return Placed(view, file, fresh, document.Insert(at, index, blank), at.Append(index));
        }

        /// <summary>Writes a section the file does not hold, with the record already inside it. Refused
        /// when the section has no key of its own — the root itself is what the file is, and a file whose
        /// root is not the shape the catalog says is not one this tool may replace.</summary>
        private static CatalogEditResult Sectioned(
            CatalogView view, CatalogFile file, bool fresh, JsonTreeDocument document, string key, JToken section, JsonPointer at)
        {
            if (key.Length == 0) return Refused(Text(Notes.NoRoom, file.Name));

            return Placed(view, file, fresh, document.Insert(JsonPointer.Root, key, section), at);
        }

        /// <summary>What the catalog holds once the write is done, and nothing at all when it refused.
        /// A file laid down for this record joins the catalog here and not before it: taken in earlier,
        /// a write the file turns down would leave the run holding a file with nothing in it, offered to
        /// the author and watched by the saver for a record that was never written.</summary>
        private static CatalogEditResult Placed(
            CatalogView view, CatalogFile file, bool fresh, bool written, JsonPointer at)
        {
            if (!written) return Refused(Text(Notes.NotWritten, file.Name));

            if (fresh) view.AddFile(file);

            view.Reread();

            return Standing(view, file, at);
        }

        /// <summary>Writes the copy into the same section as the record it was taken from, right after
        /// it: an author reads a copy where he made it, and a copy appended to the end of a long section
        /// is one he has to go looking for.
        /// <para>In a catalog written as a map the copy goes to the end of it instead: a key is added
        /// where the object ends, and the order of the keys is not what such a catalog is read by.</para></summary>
        private static CatalogEditResult Beside(CatalogView view, CatalogRecord record, JObject copy, string id)
        {
            if (view.Schema.Shape == RootShape.Single) return Refused(Notes.OneRecordOnly);
            if (record.Pointer.Parent is not { } section) return Refused(Notes.Gone);

            JsonTreeDocument document = record.File.Document;

            if (view.Schema.Shape == RootShape.Dictionary)
                return Placed(view, record.File, fresh: false, document.Insert(section, id, copy), section.Append(id));

            if (!JsonPointer.TryReadIndex(record.Pointer.Last!, out int index)) return Refused(Notes.Gone);

            int at = index + 1;

            return Placed(view, record.File, fresh: false, document.Insert(section, at, copy), section.Append(at));
        }

        /// <summary>The record the catalog now lists at that address. Said out loud when it lists none:
        /// the file was written, and an author told nothing would go on looking for the record.</summary>
        private static CatalogEditResult Standing(CatalogView view, CatalogFile file, JsonPointer at)
        {
            foreach (CatalogRecord record in view.Records)
                if (ReferenceEquals(record.File, file) && record.Pointer == at)
                    return new CatalogEditResult(record, null);

            return Refused(Text(Notes.NotListed, file.Name));
        }

        private static class Notes
        {
            public const string OneRecordOnly = "this catalog is one record and has nothing to add to.";
            public const string NoSection = "the catalog writes no section called '{0}'.";
            public const string NoId = "name the record before adding it.";
            public const string IdTaken = "'{0}' is already written in this catalog.";
            public const string IdTakenIn = "'{0}' is already written in '{1}'.";
            public const string Gone = "the record is no longer in the document.";
            public const string NotRemoved = "'{0}' could not be taken out.";
            public const string NoFileNamed = "nothing says which file the record goes to.";
            public const string UnusableName = "'{0}' is not a name a file can be given.";
            public const string NoFolder = "there is no folder at '{0}' to write '{1}' into.";
            public const string Unreadable = "{0} is on disk and was not read; it is not written over.";
            public const string NoRoom = "{0} does not hold the records where the catalog says they are.";
            public const string NotWritten = "{0} refused the record.";
            public const string NotListed = "{0} was written and the catalog does not list the record.";

            /// <summary>What one press of undo takes back, named the way the panels name a rename.</summary>
            public const string RenameStep = "rename {0} → {1}";

            public const string KeyTaken = "'{0}' already keys the same map in {1}.";
        }
    }
}
