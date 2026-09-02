namespace Tooling.Catalogs
{
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>One record of a catalog: the file it lives in, its address inside that file, the id it
    /// is listed under, and the schema it is drawn by.</summary>
    /// <remarks>The value is not held, only addressed: an edit replaces the node at the pointer, and a
    /// copy taken at load would go stale the moment that happened.</remarks>
    public sealed record CatalogRecord(CatalogFile File, JsonPointer Pointer, string Id, RecordSchema Schema)
    {
        /// <summary>What stands at the record's address now, or null when the document no longer has
        /// anything there.</summary>
        public JToken? Token => File.Document.Resolve(Pointer);
    }
}
