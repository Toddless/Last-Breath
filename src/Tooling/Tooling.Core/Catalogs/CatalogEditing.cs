namespace Tooling.Catalogs
{
    using System;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
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

            if (Refusal(view, section.Record, wanted) is { } refusal) return Refused(refusal);

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

            if (Refusal(view, record.Schema, wanted) is { } refusal) return Refused(refusal);

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

        /// <summary>Whether the catalog already writes a record under this name. Asked of the whole
        /// catalog and not of one file: the game reads every file of a folder into one table, so two
        /// records sharing a name are one record and one that never loads.</summary>
        public static bool Taken(CatalogView view, string id)
        {
            ArgumentNullException.ThrowIfNull(view);

            return view.Records.Any(record => string.Equals(record.CurrentId, id, StringComparison.OrdinalIgnoreCase));
        }

        private static CatalogEditResult Refused(string note) => new(null, note);

        /// <summary>Why a name will not do, or null when it will. A record the shape or the schema names
        /// by its id has to have one, and no catalog may write the same name twice.</summary>
        private static string? Refusal(CatalogView view, RecordSchema schema, string id)
        {
            if (CatalogRecords.Names(view.Schema, schema) && id.Length == 0) return Notes.NoId;

            return id.Length > 0 && Taken(view, id) ? Text(Notes.IdTaken, id) : null;
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
            public const string Gone = "the record is no longer in the document.";
            public const string NotRemoved = "'{0}' could not be taken out.";
            public const string NoFileNamed = "nothing says which file the record goes to.";
            public const string UnusableName = "'{0}' is not a name a file can be given.";
            public const string NoFolder = "there is no folder at '{0}' to write '{1}' into.";
            public const string Unreadable = "{0} is on disk and was not read; it is not written over.";
            public const string NoRoom = "{0} does not hold the records where the catalog says they are.";
            public const string NotWritten = "{0} refused the record.";
            public const string NotListed = "{0} was written and the catalog does not list the record.";
        }
    }
}
