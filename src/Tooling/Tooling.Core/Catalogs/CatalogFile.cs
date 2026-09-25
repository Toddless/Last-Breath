namespace Tooling.Catalogs
{
    using Tooling.Json;

    /// <summary>One json file of a catalog folder together with the document read out of it. The path
    /// travels with the document because the document does not know where it came from, and every
    /// message about a file — a note, a status line, a later save — has to name it.</summary>
    public sealed record CatalogFile(string Path, JsonTreeDocument Document)
    {
        /// <summary>The file's own name, which is what an author recognises in a message.</summary>
        public string Name => System.IO.Path.GetFileName(Path);

        /// <summary>The name without the extension: what a catalog is split by and what a file is
        /// asked for by, since the extension is the tool's and never the author's to choose.</summary>
        public string BaseName => System.IO.Path.GetFileNameWithoutExtension(Path);
    }
}
