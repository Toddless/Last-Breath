namespace Tooling.Tests.DataEditor
{
    using System.IO;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// The catalogs the data-editor tests are run against, described by hand. Hand-written on purpose:
    /// what is under test is driven by <see cref="RootShape"/>, by <see cref="RecordSchema.IdField"/>
    /// and by the order a record declares its fields in, and going through the reflector would be
    /// pinning the reflector a second time in every one of them.
    /// </summary>
    internal static class CatalogFixture
    {
        /// <summary>What a canonical file ends every line with, whatever the machine writing it would
        /// have used: the repository is normalized to LF, and so is the writer.</summary>
        public const string Newline = "\n";

        /// <summary>A catalog described by hand. The placement is one file named after the catalog unless
        /// the test is about placement itself — which file a record goes to is a rule of its own, and a
        /// test of the records has no business restating it.</summary>
        public static ICatalogDescriptor Descriptor(
            string catalog,
            RootShape shape,
            SchemaList<SectionSchema> sections,
            FilePlacement? placement = null,
            SchemaList<string> localizedSuffixes = default,
            string[]? requiredSuffixes = null) =>
            new HandWritten(
                catalog,
                shape,
                sections,
                placement ?? new SingleFilePlacement { FileName = catalog },
                localizedSuffixes,
                requiredSuffixes);

        public static SectionSchema Section(string key, RecordSchema record) => new() { Key = key, Record = record };

        public static RecordSchema Record(string? idField, params FieldSchema[] fields) => new()
        {
            TypeName = nameof(Record),
            Fields = fields,
            IdField = idField
        };

        public static FieldSchema Field(string jsonName, FieldKind kind) => new() { JsonName = jsonName, Kind = kind };

        /// <summary>A field the record cannot be written without, which is what a blank record is filled
        /// from.</summary>
        public static FieldSchema Required(string jsonName, FieldKind kind) =>
            new() { JsonName = jsonName, Kind = kind, Required = true };

        /// <summary>A written constant as a canonical file holds it: LF whatever this source was checked
        /// out with, and the newline at the end that the writer always adds — so a file laid down by a
        /// test and the same file written back by the tool can be compared as bytes.</summary>
        public static string OnDisk(string content) => content.ReplaceLineEndings(Newline) + Newline;

        public static void Write(string root, string catalog, string file, string content)
        {
            string folder = Path.Combine(root, catalog);

            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, file), OnDisk(content));
        }

        /// <summary>A catalog described in full by hand: the builder is offered and not used, which is
        /// what keeps the shapes under test out of the reflector's reach.</summary>
        private sealed class HandWritten(
            string catalog,
            RootShape shape,
            SchemaList<SectionSchema> sections,
            FilePlacement placement,
            SchemaList<string> localizedSuffixes,
            string[]? requiredSuffixes)
            : ICatalogDescriptor
        {
            public string Catalog => catalog;

            /// <summary>The schema as the test spells it. A catalog naming no required suffixes owes every
            /// one it declares, which is the schema's own reading and not a default written twice.</summary>
            public CatalogSchema Describe(ISchemaBuilder builder)
            {
                var schema = new CatalogSchema(shape, sections, localizedSuffixes, placement);

                return requiredSuffixes is { } required ? schema with { RequiredSuffixes = required } : schema;
            }
        }
    }
}
