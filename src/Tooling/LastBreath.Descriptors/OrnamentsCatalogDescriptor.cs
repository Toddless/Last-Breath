namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Ornaments catalog: one array under a key, each record found by its own id
    /// and both named and described in the localization by it. What an ornament is made of is read off
    /// <see cref="OrnamentData"/> and stated nowhere here.</summary>
    /// <remarks>An ornament points at nothing: which ability wears it is a fact about one character and
    /// lives in his save, so the catalog every character reads has no field for it.</remarks>
    public sealed class OrnamentsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="OrnamentDataRoot.Ornaments"/> is
        /// written as.</summary>
        public const string RecordsKey = "ornaments";

        /// <summary>Json name of the field carrying a record's id — what <see cref="OrnamentData.Id"/> is
        /// written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field naming the tier of socket the ornament grants — what
        /// <see cref="OrnamentData.Tier"/> is written as. A record declaring nothing above zero is refused
        /// at load: a socket of no tier is not one.</summary>
        public const string TierField = "tier";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "Ornaments";

        public string Catalog => DataCatalog.Ornaments;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema ornament = builder.Record(typeof(OrnamentData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = ornament }],
                // A name and a description: an ornament is a named artefact read in a tooltip, not a row
                // in a list of materials.
                [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
