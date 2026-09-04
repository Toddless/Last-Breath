namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Data.GameData;
    using Newtonsoft.Json;
    using Tooling.Catalogs;
    using static Tooling.Text.Format;

    /// <summary>
    /// The documents a tool has open, handed to the game's own readers. One place, because everything
    /// this adapter does with the narrative starts the same way — the catalog as it is written THIS
    /// second, read by the loader that will read it in the game — and a second copy of that step would
    /// be a second answer to what a dialogue currently says.
    /// </summary>
    public static class NarrativeDocuments
    {
        private const string ReadFailedFormat = "{0} could not be read: {1}";

        private const string NoCatalogFormat = "the {0} catalog is not among the ones this run opened";

        /// <summary>The open documents of one catalog, as the json the game's readers take. A catalog this
        /// run never opened is a note rather than an empty list passed off as a read one.</summary>
        public static IReadOnlyList<GameDataFile> Open(CatalogWorkspace workspace, string catalog, ICollection<string> notes)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentNullException.ThrowIfNull(notes);

            CatalogView? view = workspace.Catalogs.FirstOrDefault(entry => string.Equals(entry.Catalog, catalog, StringComparison.Ordinal));
            if (view is not null)
                return [.. view.Files.Select(file => new GameDataFile(file.Name, file.Document.Root.ToString(Formatting.None)))];

            // Said once however many readers ask for the same catalog: the fact is about the run, and a
            // second reading of one folder is not a second thing missing.
            Said(notes, Text(NoCatalogFormat, catalog));
            return [];
        }

        /// <summary>Hands one catalog's open documents to the game's own provider.</summary>
        public static void Read(CatalogWorkspace workspace, string catalog, IGameDataParticipant participant, ICollection<string> notes)
        {
            ArgumentNullException.ThrowIfNull(participant);

            foreach (GameDataFile file in Open(workspace, catalog, notes))
                Guarded(file.FileName, () => participant.Apply(catalog, file), notes);
        }

        /// <summary>Hands one FOLDER's files to the game's own provider — the way a catalog no schema
        /// describes is read, since the workspace never opened it.</summary>
        public static void Read(string folder, string catalog, IGameDataParticipant participant, ICollection<string> notes)
        {
            ArgumentNullException.ThrowIfNull(participant);
            ArgumentNullException.ThrowIfNull(notes);

            foreach (string path in CatalogWorkspace.FilePaths(folder))
                Guarded(
                    Path.GetFileName(path),
                    () => participant.Apply(catalog, new GameDataFile(Path.GetFileName(path), File.ReadAllText(path))),
                    notes);
        }

        /// <summary>One document read, or one note saying why it was not. The providers already drop a
        /// broken record on their own; this catches the file that is not the shape they expect at all.</summary>
        private static void Guarded(string name, Action read, ICollection<string> notes)
        {
            try
            {
                read();
            }
            catch (Exception failure) when (failure is IOException or JsonException or InvalidOperationException
                                               or ArgumentException)
            {
                notes.Add(Text(ReadFailedFormat, name, failure.Message));
            }
        }

        /// <summary>Adds a note the run has not already made. A tool reads one catalog for several
        /// questions, and the same missing folder said once per question is noise about the run.</summary>
        private static void Said(ICollection<string> notes, string note)
        {
            if (notes.Contains(note)) return;

            notes.Add(note);
        }
    }
}
