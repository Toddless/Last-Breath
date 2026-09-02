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

        /// <summary>How many catalogs the game has not described yet. Named on the status line so the
        /// tool says what it cannot draw instead of looking as though the data ends here.</summary>
        public static int UndescribedCount => CatalogDescriptors.NotYetDescribed.Count;
    }
}
