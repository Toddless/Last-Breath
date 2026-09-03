namespace Tooling.Catalogs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Tooling.Json;
    using Tooling.Schema.Reflection;
    using static Tooling.Text.Format;

    /// <summary>What a save did: the files it wrote, and what it could not write and why.</summary>
    public sealed record CatalogSaveResult(IReadOnlyList<string> Saved, IReadOnlyList<string> Notes);

    /// <summary>
    /// What a run has changed and how it gets back to disk. Godot-free like everything else the host
    /// draws from: the host asks what is unsaved and says so, it never walks the files itself.
    /// <para>Whether a file is dirty is asked of its own history, so stepping back onto the state that
    /// was written makes it clean again — an answer that a flag, which can only ever be turned on,
    /// could not give.</para>
    /// <para>The questions about one catalog are static because they are functions of it and of nothing
    /// else. The instance exists for the one thing a caller cannot work out from a view — that some
    /// history, in some file, has just moved.</para>
    /// </summary>
    public sealed class CatalogSaver
    {
        private readonly CatalogWorkspace _workspace;

        public CatalogSaver(CatalogWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            _workspace = workspace;

            foreach (CatalogFile file in Files(workspace)) Watch(file);

            // A file the run lays down is watched the same way the ones read from disk are: it is dirty
            // from its first record onwards, and a status line that never heard of it would say the run
            // has nothing to write while a whole file waits to be created.
            foreach (CatalogView catalog in workspace.Catalogs) catalog.FileAdded += Watch;
        }

        /// <summary>A history moved: something was edited, undone, redone or written out. What is unsaved
        /// and what the next step back would be are both read after this, so one event answers for both.</summary>
        public event Action? Changed;

        /// <summary>Whether anything at all in the run is waiting to be written.</summary>
        public bool AnyDirty => Files(_workspace).Any(IsDirty);

        /// <summary>How many files of the whole run are waiting to be written.</summary>
        public int DirtyCount => Files(_workspace).Count(IsDirty);

        public static bool IsDirty(CatalogFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return !file.Document.History.IsClean;
        }

        public static bool IsDirty(CatalogView view)
        {
            ArgumentNullException.ThrowIfNull(view);

            return view.Files.Any(IsDirty);
        }

        /// <summary>
        /// Writes back every file of one catalog that has changed, in the shape a canonical file has and
        /// with its keys in the order the catalog's own schema declares them.
        /// <para>A file that will not be written is a note and not an exception: the rest of the catalog
        /// still has to reach disk, and a save that gave up on the first read-only file would leave the
        /// author with no way to keep the work it had already written out.</para>
        /// </summary>
        public static CatalogSaveResult SaveDirty(CatalogView view)
        {
            ArgumentNullException.ThrowIfNull(view);

            List<string> saved = [];
            List<string> notes = [];

            // One order per catalog rather than per file: it caches the layout of every record it has
            // been asked about, and the files of one catalog are written by the same schema.
            var order = new SchemaKeyOrder(view.Schema);

            foreach (CatalogFile file in view.Files)
            {
                if (!IsDirty(file)) continue;

                if (Save(file, order) is { } refusal) notes.Add(Text(Notes.Unwritable, file.Name, refusal));
                else saved.Add(file.Path);
            }

            return new CatalogSaveResult(saved, notes);
        }

        /// <summary>Writes back everything the run has changed, in every catalog. The author edits a
        /// record, not a file, and a save that only covered the catalog on screen would leave work
        /// behind in the one looked at a minute ago.</summary>
        public CatalogSaveResult SaveAll()
        {
            List<string> saved = [];
            List<string> notes = [];

            foreach (CatalogView view in _workspace.Catalogs)
            {
                CatalogSaveResult result = SaveDirty(view);

                saved.AddRange(result.Saved);
                notes.AddRange(result.Notes.Select(note => Text(Notes.Named, view.Catalog, note)));
            }

            return new CatalogSaveResult(saved, notes);
        }

        private static IEnumerable<CatalogFile> Files(CatalogWorkspace workspace) =>
            workspace.Catalogs.SelectMany(catalog => catalog.Files);

        /// <summary>Writes one file, or says why it stayed where it was. Everything a path can refuse —
        /// gone, taken by a folder, read-only, too long — arrives as one of these; a number no json can
        /// spell arrives as an argument being out of range, which is the same kind of answer.</summary>
        private static string? Save(CatalogFile file, SchemaKeyOrder order)
        {
            try
            {
                file.Document.Save(file.Path, order, CanonicalJsonOptions.Default);
                return null;
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
                                                or ArgumentException or NotSupportedException)
            {
                return failure.Message;
            }
        }

        private void Watch(CatalogFile file) => file.Document.History.Changed += Raise;

        private void Raise() => Changed?.Invoke();

        private static class Notes
        {
            public const string Named = "{0}: {1}";
            public const string Unwritable = "{0} could not be written: {1}";
        }
    }
}
