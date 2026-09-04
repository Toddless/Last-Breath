namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Data.ReputationData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the ReputationPerks catalog: one array under a key, each record found by the
    /// standing level it speaks for. What a level hands out is read off
    /// <see cref="ReputationPerkLevelEntry"/> and stated nowhere here.</summary>
    /// <remarks>The address is a level and not an id of its own: a level lists its COMPLETE set with no
    /// inheritance between levels, so a second record for one level is a set the reader overwrites.</remarks>
    public sealed class ReputationPerksCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="ReputationPerksData.Levels"/> is
        /// written as.</summary>
        public const string RecordsKey = "levels";

        /// <summary>Json name of the field a record is found by — what
        /// <see cref="ReputationPerkLevelEntry.Level"/> is written as.</summary>
        public const string IdField = "level";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "ReputationPerks";

        public string Catalog => DataCatalog.ReputationPerks;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema level = builder.Record(typeof(ReputationPerkLevelEntry)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = level }],
                // Tuning: the perks are read by the systems that spend them, and the level the player is
                // told about is worded by its own enum member.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
