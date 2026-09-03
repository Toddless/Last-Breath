namespace Tooling.Catalogs
{
    using System;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>One record of a catalog: the file it lives in, its address inside that file, the id it
    /// was listed under when the file was read, the schema it is drawn by, and the key of the section it
    /// stands in — empty when the section is the root of the file itself.</summary>
    /// <remarks>The value is not held, only addressed: an edit replaces the node at the pointer, and a
    /// copy taken at load would go stale the moment that happened.</remarks>
    public sealed record CatalogRecord(CatalogFile File, JsonPointer Pointer, string Id, RecordSchema Schema, string Section)
    {
        /// <summary>What stands at the record's address now, or null when the document no longer has
        /// anything there.</summary>
        public JToken? Token => File.Document.Resolve(Pointer);

        /// <summary>
        /// The id the record is written under now. The id is a field like any other and an author may
        /// retype it, so anything naming a record to its author — a list row, a heading — has to ask
        /// again rather than repeat the word the file held when it was opened.
        /// <para>A record whose schema names no id field, or whose id field is empty, keeps the name it
        /// was listed under: that name was its place in the section, and its place has not changed.</para>
        /// </summary>
        public string CurrentId => WrittenId(Schema, Token) ?? Id;

        /// <summary>The id a record is written under, or null when it carries none. An id written as
        /// something other than text is read the way the file holds it: it is the word the author
        /// searches the file for, and no machine's culture has a say in it.</summary>
        public static string? WrittenId(RecordSchema schema, JToken? token)
        {
            ArgumentNullException.ThrowIfNull(schema);

            return schema.IdField is { } name
                   && token is JObject holder
                   && holder.TryGetValue(name, StringComparison.Ordinal, out JToken? value)
                   && value is JValue { Value: not null }
                   && JsonScalars.Written(value) is { Length: > 0 } written
                ? written
                : null;
        }
    }
}
