namespace LastBreath.Descriptors
{
    using System;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// A catalog whose file is one settings document: no list, no ids, nothing to add to — the root
    /// object IS the record, and the tool shows it as the single entry it is.
    /// <para>Written once because the shape is one shape: a dozen catalogs differ only in the name of
    /// the folder, the DTO the file is parsed into and the name of the file itself, and a dozen copies
    /// of the same four lines would be a dozen places for the shape to drift.</para>
    /// </summary>
    /// <remarks>A settings document carries no author-facing text of its own: its fields are named by
    /// the tool from the schema, and nothing in it is keyed off an id, because it has none.</remarks>
    public abstract class SingleObjectDescriptor : ICatalogDescriptor
    {
        /// <summary>The key of the one section: a settings document stands at the root of its file,
        /// and the root is what the empty key names.</summary>
        public const string RootKey = "";

        private readonly Type _dtoType;

        /// <param name="catalog">Folder the catalog is known by.</param>
        /// <param name="dtoType">The type the game parses the file into.</param>
        /// <param name="fileName">The one file, without extension. Written out rather than taken from
        /// the catalog's name: the two need not agree, and they are two different facts.</param>
        protected SingleObjectDescriptor(string catalog, Type dtoType, string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(catalog);
            ArgumentNullException.ThrowIfNull(dtoType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            Catalog = catalog;
            FileName = fileName;
            _dtoType = dtoType;
        }

        public string Catalog { get; }

        /// <summary>The one file of the catalog, without extension.</summary>
        public string FileName { get; }

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return new CatalogSchema(
                RootShape.Single,
                [new SectionSchema { Key = RootKey, Record = builder.Record(_dtoType) }],
                // Tuning, and tuning is not shown: nothing here is keyed off an id it does not have.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
