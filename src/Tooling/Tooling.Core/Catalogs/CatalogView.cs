namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
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

        /// <summary>Takes a file into the catalog. The document is the run's from here on: it is written
        /// with the rest on the next save, and it is on its own history that its edits are taken back.</summary>
        public void AddFile(CatalogFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

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
