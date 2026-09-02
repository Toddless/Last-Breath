namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>
    /// Finds the records of one file by the shape its catalog is written in. The shape is the only
    /// thing that says where records sit — an array under a key, several such arrays, a map, or the
    /// document itself — so reading them is one walk driven by <see cref="RootShape"/> and not a habit
    /// each catalog repeats.
    /// </summary>
    public static class CatalogRecords
    {
        /// <summary>Stands in for the id of a record that carries none, so every row still has a name
        /// the author can point at.</summary>
        private const string IndexIdFormat = "#{0}";

        /// <summary>Reads every record of <paramref name="file"/>. A section the file does not hold is
        /// no failure — a catalog may be split so that each file writes some of them — but a section
        /// holding something other than what its shape promises is noted rather than skipped in
        /// silence.</summary>
        public static IReadOnlyList<CatalogRecord> Read(CatalogSchema schema, CatalogFile file, IList<string> notes)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(file);
            ArgumentNullException.ThrowIfNull(notes);

            List<CatalogRecord> records = [];

            foreach (SectionSchema section in schema.Sections)
            {
                JsonPointer at = Locate(section.Key);
                JToken? token = file.Document.Resolve(at);

                if (token is null) continue;

                switch (schema.Shape)
                {
                    case RootShape.ArrayUnderKey:
                    case RootShape.SectionsOfArrays:
                        ReadArray(records, file, at, section, token, notes);
                        break;
                    case RootShape.Dictionary:
                        ReadMap(records, file, at, section, token, notes);
                        break;
                    case RootShape.Single:
                        ReadSingle(records, file, at, section, token, notes);
                        break;
                    default:
                        notes.Add(Text(Notes.UnknownShape, file.Name, schema.Shape));
                        break;
                }
            }

            return records;
        }

        /// <summary>Where a section stands in its file: under its key, or at the root itself when the
        /// section has no key of its own.</summary>
        private static JsonPointer Locate(string key) =>
            string.IsNullOrEmpty(key) ? JsonPointer.Root : JsonPointer.Root.Append(key);

        private static void ReadArray(
            List<CatalogRecord> records, CatalogFile file, JsonPointer at, SectionSchema section, JToken token, IList<string> notes)
        {
            if (token is not JArray array)
            {
                notes.Add(Text(Notes.NotAnArray, file.Name, Named(at)));
                return;
            }

            for (int index = 0; index < array.Count; index++)
                records.Add(new CatalogRecord(file, at.Append(index), Id(section.Record, array[index], index), section.Record));
        }

        private static void ReadMap(
            List<CatalogRecord> records, CatalogFile file, JsonPointer at, SectionSchema section, JToken token, IList<string> notes)
        {
            if (token is not JObject map)
            {
                notes.Add(Text(Notes.NotAMap, file.Name, Named(at)));
                return;
            }

            foreach (JProperty pair in map.Properties())
                records.Add(new CatalogRecord(file, at.Append(pair.Name), pair.Name, section.Record));
        }

        /// <summary>A file that is one record. It still gets an id, so a list of records reads the same
        /// whatever the catalog's shape.</summary>
        private static void ReadSingle(
            List<CatalogRecord> records, CatalogFile file, JsonPointer at, SectionSchema section, JToken token, IList<string> notes)
        {
            if (token is not JObject)
            {
                notes.Add(Text(Notes.NotARecord, file.Name, Named(at)));
                return;
            }

            records.Add(new CatalogRecord(file, at, Id(section.Record, token, index: 0), section.Record));
        }

        /// <summary>The id the record is written under, or its place in the section when it carries
        /// none — a settings document, a position at a loot table. Reading the id is the record's own
        /// rule, because a record is asked the same question again every time it is named to its
        /// author; only the standing-in name is decided here, where the place is known.</summary>
        private static string Id(RecordSchema schema, JToken token, int index) =>
            CatalogRecord.WrittenId(schema, token) ?? Text(IndexIdFormat, index);

        private static string Named(JsonPointer pointer) => pointer.IsRoot ? Notes.RootName : pointer.ToString();

        private static class Notes
        {
            /// <summary>What the root is called in a message; it has no address of its own.</summary>
            public const string RootName = "the document";

            public const string NotAnArray = "{0}: '{1}' holds no array of records.";
            public const string NotAMap = "{0}: '{1}' holds no map of records.";
            public const string NotARecord = "{0}: '{1}' is not a single record.";
            public const string UnknownShape = "{0}: the shape '{1}' is one this build cannot read.";
        }
    }
}
