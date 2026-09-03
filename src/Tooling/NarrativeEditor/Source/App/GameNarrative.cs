namespace NarrativeEditor.Source.App
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.GameData;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Narrative;
    using Tooling.Schema;
    using static Tooling.Text.Format;

    /// <summary>
    /// The one place this host touches the game: which catalogs hold the narrative, and how a record of
    /// one of them is read as an outline. Below it the tool works on schemas and json documents alone.
    /// </summary>
    /// <remarks>
    /// The conditions and actions written into a dialogue or a quest are free-form json here, and the
    /// inspector draws them as such. The vocabulary they would be drawn from is the narrative
    /// factories' own — <see cref="NarrativeSchemas"/> turns those into record schemas — but the
    /// factories are built over live game services in the game's own service provider, so nothing
    /// outside a running game can list them yet.
    /// </remarks>
    public static class GameNarrative
    {
        /// <summary>How the run's report names the catalog a note belongs to.</summary>
        private const string NamedFormat = "{0}: ";

        /// <summary>The catalogs this tool lists, in the order it lists them.</summary>
        public static IReadOnlyList<string> Catalogs { get; } = [DataCatalog.Dialogues, DataCatalog.Quests];

        /// <summary>
        /// Reads the whole data root, not the narrative alone. A dialogue names npcs, items and quests,
        /// and a reference is only checked — and only offered as a list to pick from — against a catalog
        /// the run has read; a workspace of two catalogs would leave every id in the narrative unanswered.
        /// </summary>
        public static CatalogWorkspace Load(string root) => CatalogWorkspace.Load(root, CatalogDescriptors.All);

        /// <summary>The catalogs of the narrative as this run read them, in the order they are listed.
        /// A catalog the build describes no schema for is not among them.</summary>
        public static IReadOnlyList<CatalogView> Narrative(CatalogWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            return [.. Catalogs.SelectMany(catalog => workspace.Catalogs.Where(view => Named(view, catalog)))];
        }

        /// <summary>
        /// What the narrative catalogs could not answer. Only theirs: the run reads the whole data root
        /// for the ids the narrative points at, and a note about a catalog this tool does not open is one
        /// its author can do nothing with.
        /// <para>Taken out of the run's own report rather than off the catalogs, because a catalog whose
        /// schema could not be built at all is not among them — and that note is the one an author of an
        /// empty-looking tool most needs to read. Every note of the report is named by the catalog it
        /// belongs to, which is what makes it possible to say which are these two's.</para>
        /// </summary>
        public static IReadOnlyList<string> Report(CatalogWorkspace workspace)
        {
            ArgumentNullException.ThrowIfNull(workspace);

            return [.. workspace.Report.Where(Ours)];
        }

        /// <summary>
        /// One record read as the structure its author works in: a dialogue by its nodes, a quest by its
        /// stages. Null for a record that is no longer in its document, and for a catalog that is not one
        /// of the narrative's — the outline of a record is a reading of that record's own shape, and there
        /// is no general one.
        /// </summary>
        public static OutlineNode? Outlined(CatalogView view, CatalogRecord record)
        {
            ArgumentNullException.ThrowIfNull(view);
            ArgumentNullException.ThrowIfNull(record);

            if (record.Token is not { } token) return null;
            if (Named(view, DataCatalog.Dialogues)) return Outline.Dialogue(record.Schema, token, record.Pointer);
            if (Named(view, DataCatalog.Quests)) return Outline.Quest(record.Schema, token, record.Pointer);

            return null;
        }

        /// <summary>Whether a note of the run belongs to one of the catalogs this tool edits.</summary>
        private static bool Ours(string note) =>
            Catalogs.Any(catalog => note.StartsWith(Text(NamedFormat, catalog), StringComparison.Ordinal));

        private static bool Named(CatalogView view, string catalog) =>
            string.Equals(view.Catalog, catalog, StringComparison.Ordinal);
    }
}
