namespace Tooling.Schema.Model
{
    /// <summary>
    /// Where a reference may point: a catalog, and at most one section of it. A catalog holding sections
    /// that answer to nothing each other — the material categories beside the materials, the abilities
    /// beside the augments — offers a field naming the whole of it far more ids than the game will
    /// resolve, and marks nothing as broken when the author picks one of them.
    /// </summary>
    /// <remarks>A section that is not named is not the same as a catalog with one section: the target
    /// says whether the narrowing was DECIDED, and every catalog may grow a second section later.</remarks>
    public sealed record ReferenceTarget(string Catalog, string? Section = null)
    {
        /// <summary>What stands between a catalog and the section of it a target names.</summary>
        public const string SectionSeparator = "/";

        /// <summary>The whole of a catalog: every section of it answers. Written out rather than left to
        /// the second argument, so that "no section" reads as a decision wherever it is made.</summary>
        public static ReferenceTarget Whole(string catalog) => new(catalog);

        public string Catalog { get; init; } = SchemaGuard.Text(Catalog, nameof(Catalog));

        /// <summary>The section key the ids must be written under, as the file writes it; null when every
        /// record of the catalog answers. Never empty: the root of a one-section catalog narrows nothing,
        /// so naming it would be a decision with no content.</summary>
        public string? Section { get; init; } = Section is null ? null : SchemaGuard.Text(Section, nameof(Section));

        /// <summary>How a target is named to an author: <c>Resources/materialCategories</c>.</summary>
        public override string ToString() => Section is null ? Catalog : $"{Catalog}{SectionSeparator}{Section}";
    }
}
