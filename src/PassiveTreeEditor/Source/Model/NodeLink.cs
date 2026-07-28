namespace PassiveTreeEditor.Source.Model
{
    /// <summary>
    /// An undirected edge. Endpoints are normalized on creation so that A→B and B→A are the same
    /// value: that keeps the set free of mirror duplicates and gives the save file a stable order.
    /// Always build one through <see cref="Between"/>.
    /// </summary>
    public readonly record struct NodeLink(string A, string B)
    {
        public static NodeLink Between(string first, string second) =>
            string.CompareOrdinal(first, second) <= 0 ? new NodeLink(first, second) : new NodeLink(second, first);
    }
}
