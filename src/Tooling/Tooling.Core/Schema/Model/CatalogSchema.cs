namespace Tooling.Schema.Model
{
    /// <summary>What one catalog's file looks like: its shape, the records under each section, the
    /// localization keys hanging off a record's id, and which file a new record is written to.</summary>
    /// <remarks>The catalog is not named here — the descriptor that produced this names it, and a
    /// second copy of the name is a second thing to keep in step.</remarks>
    public sealed record CatalogSchema
    {
        public CatalogSchema(
            RootShape shape,
            SchemaList<SectionSchema> sections,
            SchemaList<string> localizedSuffixes,
            FilePlacement placement)
        {
            Shape = shape;
            Sections = sections;
            LocalizedSuffixes = localizedSuffixes;
            Placement = placement;
        }

        public RootShape Shape
        {
            get => field;
            init
            {
                SchemaGuard.Holds(value, Sections);
                field = value;
            }
        }

        public SchemaList<SectionSchema> Sections
        {
            get => field;
            init
            {
                SchemaGuard.Holds(Shape, SchemaGuard.Sections(value));
                field = value;
            }
        }

        /// <summary>Appended to a record's id to build its localization keys; the empty suffix is the
        /// name itself. Empty when the catalog carries no author-facing text.</summary>
        public SchemaList<string> LocalizedSuffixes { get; init; }

        public FilePlacement Placement
        {
            get => field;
            init => field = SchemaGuard.NotNull(value, nameof(Placement));
        }
    }
}
