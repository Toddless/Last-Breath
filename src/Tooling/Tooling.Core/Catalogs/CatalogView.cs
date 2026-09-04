namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using Tooling.Editing.History;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>One catalog as the tool holds it: what describes it, the schema built from that, the
    /// files it is written across, its records in the order they stand in those files, and everything
    /// that could not be answered while it was read.</summary>
    /// <remarks>The files and the records are what an authoring run changes: a record added to a catalog
    /// split by a field of its own may bring a file with it, and every record written or taken out moves
    /// the addresses of the ones below it.</remarks>
    public sealed class CatalogView
    {
        private readonly List<CatalogFile> _files = [];

        private IReadOnlyList<CatalogRecord> _records = [];

        /// <summary>A file the run laid down and the folder did not hold. Whoever watches the documents
        /// of this catalog — the saver, an index of ids — has one more to watch.</summary>
        public event Action<CatalogFile>? FileAdded;

        public required ICatalogDescriptor Descriptor { get; init; }

        public required CatalogSchema Schema { get; init; }

        /// <summary>The folder this catalog is read from and written to. Held rather than worked out
        /// from the files: a catalog whose folder is there and empty is one an author may write the
        /// first record of, and a file laid down beside an existing one would land in whatever nested
        /// folder that one happens to sit in.</summary>
        public required string Folder { get; init; }

        /// <summary>The stack this catalog's files record onto, or null where each keeps its own. Held so
        /// that a file the run lays down joins it too: the gesture that created a file is written into that
        /// file, and a step left on a stack nothing steps is a gesture nobody can take back.</summary>
        public EditHistory? History { get; init; }

        public required IReadOnlyList<CatalogFile> Files
        {
            get => _files;
            init => _files.AddRange(value);
        }

        public required IReadOnlyList<CatalogRecord> Records
        {
            get => _records;
            init => _records = value;
        }

        /// <summary>Notes of this catalog alone, unnamed: the reflector's, the schema checks', and
        /// whatever a file refused to answer. Named by catalog in
        /// <see cref="CatalogWorkspace.Report"/>.</summary>
        public required IReadOnlyList<string> Notes { get; init; }

        public string Catalog => Descriptor.Catalog;

        /// <summary>Whether the catalog lists this record itself rather than a row standing inside one. A
        /// node of a conversation and a stage of a quest are named by nothing outside their own file: no
        /// section lists them, no map is keyed by them, and a rename of one carries no reference.</summary>
        public bool Lists(CatalogRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            foreach (CatalogRecord listed in _records)
                if (listed.SameAs(record))
                    return true;

            return false;
        }

        /// <summary>Takes a file into the catalog. The document is the run's from here on: it is written
        /// with the rest on the next save, and it joins the catalog's history bringing the step that wrote
        /// it along — a file is laid down by being written into, and that first record is as much a thing
        /// to take back as any other.</summary>
        public void AddFile(CatalogFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            if (History is { } history) file.Document.Follow(history);

            _files.Add(file);
            FileAdded?.Invoke(file);
        }

        /// <summary>Reads the records of every file again. A record written or taken out shifts the
        /// addresses of everything below it in its section, and a list rebuilt whole is the one answer
        /// that cannot leave one of them pointing at its neighbour.</summary>
        /// <remarks>What the walk has to say is dropped: a file said it once when it was read, and
        /// nothing the tool writes can turn a section into another shape.</remarks>
        public void Reread()
        {
            List<CatalogRecord> records = [];
            List<string> unheard = [];

            foreach (CatalogFile file in _files) records.AddRange(CatalogRecords.Read(Schema, file, unheard));

            _records = records;
        }
    }
}
