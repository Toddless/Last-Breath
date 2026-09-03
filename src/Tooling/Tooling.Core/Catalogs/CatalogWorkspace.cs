namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;
    using static Tooling.Text.Format;

    /// <summary>
    /// Everything one run of an authoring tool has open: a data root, the catalogs described for it,
    /// and the documents read out of them. Godot-free on purpose — the host only draws what is
    /// gathered here, and the gathering is what the tests exercise.
    /// <para>Nothing is thrown for a catalog that will not read. A folder that is not there, a file
    /// that is not json, a section that is not shaped the way its schema says: each is a note, and the
    /// tool opens showing the catalogs that did read.</para>
    /// </summary>
    public sealed class CatalogWorkspace
    {
        /// <summary>What a catalog's files are named with — the one place the extension is written.</summary>
        public const string FileExtension = ".json";

        private const string FileSearchPattern = "*" + FileExtension;

        private CatalogWorkspace(string root, IReadOnlyList<CatalogView> catalogs, IReadOnlyList<string> report)
        {
            Root = root;
            Catalogs = catalogs;
            Report = report;
        }

        /// <summary>The folder the catalogs were read from.</summary>
        public string Root { get; }

        /// <summary>The catalogs that could be described, in the order their descriptors were given.</summary>
        public IReadOnlyList<CatalogView> Catalogs { get; }

        /// <summary>Every note of the run, each naming the catalog it belongs to.</summary>
        public IReadOnlyList<string> Report { get; }

        public int FileCount => Catalogs.Sum(catalog => catalog.Files.Count);

        public int RecordCount => Catalogs.Sum(catalog => catalog.Records.Count);

        public static CatalogWorkspace Load(string root, IEnumerable<ICatalogDescriptor> descriptors)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(root);
            ArgumentNullException.ThrowIfNull(descriptors);

            List<CatalogView> catalogs = [];
            List<string> report = [];

            foreach (ICatalogDescriptor descriptor in descriptors)
            {
                if (Read(root, descriptor, report) is not { } view) continue;

                catalogs.Add(view);
                report.AddRange(view.Notes.Select(note => Text(Notes.Named, view.Catalog, note)));
            }

            return new CatalogWorkspace(root, catalogs, report);
        }

        /// <summary>The json files of one catalog, in the order the game's own data source reads them,
        /// nested folders included. Empty for a folder that is not there — an absent catalog is an
        /// answer the caller reports, not an exception it has to catch.</summary>
        public static IReadOnlyList<string> FilePaths(string folder)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(folder);

            if (!Directory.Exists(folder)) return [];

            return
            [
                .. Directory.EnumerateFiles(folder, FileSearchPattern, SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            ];
        }

        public static string Folder(string root, string catalog) => Path.Combine(root, catalog);

        /// <summary>Reads one catalog. Null when its schema could not be built at all — there is
        /// nothing to draw it with, and the reason is put on the run's report instead.
        /// <para>Anything a describer throws is that reason. A descriptor is code from outside this
        /// library, and the promise made above it — that one catalog costs itself and no more — cannot
        /// be kept by naming in advance the exceptions someone else's walk over someone else's types
        /// might raise.</para></summary>
        private static CatalogView? Read(string root, ICatalogDescriptor descriptor, List<string> report)
        {
            var builder = new CatalogSchemaBuilder();
            CatalogSchema schema;

            try
            {
                schema = builder.Build(descriptor);
            }
            catch (Exception broken)
            {
                report.Add(Text(Notes.Undescribed, descriptor.Catalog, broken.Message));
                return null;
            }

            List<string> notes = [.. builder.Reflection.Notes, .. builder.Checks.Notes];
            List<CatalogFile> files = [];
            List<CatalogRecord> records = [];
            string folder = Folder(root, descriptor.Catalog);
            bool exists = Directory.Exists(folder);

            IReadOnlyList<string> paths = FilePaths(folder);

            if (!exists) notes.Add(Text(Notes.NoFolder, folder));

            // An empty folder is worth saying only when the folder is there — otherwise it is the same
            // fact twice. It is asked of the paths and not of what was read: a folder whose files all
            // refused to parse holds files, and has already said so once per file.
            else if (paths.Count == 0) notes.Add(Text(Notes.NoFiles, folder, FileExtension));

            foreach (string path in paths)
            {
                if (Open(path, notes) is not { } file) continue;

                files.Add(file);
                records.AddRange(CatalogRecords.Read(schema, file, notes));
            }

            return new CatalogView
            {
                Descriptor = descriptor,
                Schema = schema,
                Folder = folder,
                Files = files,
                Records = records,
                Notes = notes
            };
        }

        /// <summary>Reads one file, or notes why it could not be read and hands back nothing. A broken
        /// file costs its own records and no more.</summary>
        private static CatalogFile? Open(string path, List<string> notes)
        {
            try
            {
                return new CatalogFile(path, JsonTreeDocument.Load(path));
            }
            catch (Exception failure) when (failure is FormatException or IOException or UnauthorizedAccessException)
            {
                notes.Add(Text(Notes.Unreadable, Path.GetFileName(path), failure.Message));
                return null;
            }
        }

        private static class Notes
        {
            public const string Named = "{0}: {1}";
            public const string Undescribed = "{0}: the schema could not be built: {1}";
            public const string NoFolder = "there is no folder at '{0}'.";
            public const string NoFiles = "'{0}' holds no {1} file.";
            public const string Unreadable = "{0} could not be read: {1}";
        }
    }
}
