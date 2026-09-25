namespace Tooling.Schema.Model
{
    /// <summary>What one catalog's file looks like: its shape, the records under each section, the
    /// localization keys hanging off a record's id, and which file a new record is written to.</summary>
    /// <remarks>The catalog is not named here — the descriptor that produced this names it, and a
    /// second copy of the name is a second thing to keep in step.</remarks>
    public sealed record CatalogSchema
    {
        /// <summary>Which suffixes the catalog OWES, when it says so. Unset is not "none": a catalog that
        /// says nothing owes every key it declares.</summary>
        private readonly SchemaList<string>? _required;

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

        /// <summary>
        /// Which of those keys a record OWES in every locale, as against the ones it MAY be worded under.
        /// The two are not the same list: a box the tool offers and a rename carries along stands for a key
        /// that can be there, while an unwritten key is only a fact about the data where the game reads
        /// that key for every record — an effect's standing card is written for the effects a description
        /// links to, and holding all of them to one would owe a hundred keys nobody asked for.
        /// </summary>
        /// <remarks>Unset means every declared suffix: a catalog saying nothing about it owes its wording
        /// whole, and leaving a key optional is the decision that has to be written down. Only what the
        /// catalog declares may be owed — a suffix required and never offered is a key no record is worded
        /// under, which is what a misspelling of one of them looks like from the outside.</remarks>
        public SchemaList<string> RequiredSuffixes
        {
            get => _required ?? LocalizedSuffixes;
            init => _required = SchemaGuard.Among(value, LocalizedSuffixes, nameof(RequiredSuffixes));
        }

        public FilePlacement Placement
        {
            get => field;
            init => field = SchemaGuard.NotNull(value, nameof(Placement));
        }
    }
}
