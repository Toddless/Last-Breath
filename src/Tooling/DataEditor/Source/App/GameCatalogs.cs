namespace DataEditor.Source.App
{
    using Core.Data.Schema;
    using Tooling.Catalogs;

    /// <summary>
    /// The one place this host touches the game: the catalogs it can be handed a schema for. Below it
    /// the tool works on schemas and json documents alone, which is what keeps the reading of a folder
    /// testable outside Godot.
    /// </summary>
    public static class GameCatalogs
    {
        public static CatalogWorkspace Load(string root) => CatalogWorkspace.Load(root, CatalogDescriptors.All);

        /// <summary>How many catalogs the game has at all, described or not. It is the denominator of
        /// the status line: counted from the descriptors themselves, a catalog whose schema refused to
        /// build would leave the whole it is part of, and the line would say the data ends here.</summary>
        public static int TotalCount => CatalogDescriptors.All.Count + CatalogDescriptors.NotYetDescribed.Count;
    }
}
