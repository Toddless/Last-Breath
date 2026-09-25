namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Data.ReputationData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the ReputationDeeds catalog: one array under a key, each deed found by its own
    /// id. What a deed is made of is read off <see cref="ReputationDeedEntry"/> and stated nowhere
    /// here.</summary>
    /// <remarks>One number stands at the root beside the records — how far a bystander may be and still
    /// learn of the deed. It belongs to the catalog as a whole rather than to any deed in it, and the shape
    /// has room for sections of records and none for a number: the tool carries it through as the file had
    /// it, and <see cref="WitnessRadiusField"/> is named so that carrying it is a decision.</remarks>
    public sealed class ReputationDeedsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="ReputationDeedsData.Deeds"/> is
        /// written as.</summary>
        public const string RecordsKey = "deeds";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="ReputationDeedEntry.Id"/> is written as. It is what the deed is published under:
        /// a dialogue naming a word no deed writes changes nobody's standing.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the one tuning value standing at the root beside the records — what
        /// <see cref="ReputationDeedsData.WitnessRadius"/> is written as.</summary>
        public const string WitnessRadiusField = "witnessRadius";

        /// <summary>Json name of the anti-death-spiral floor — what
        /// <see cref="ReputationDeedEntry.NoPenaltyAtOrBelow"/> is written as.</summary>
        public const string FloorField = "noPenaltyAtOrBelow";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "ReputationDeeds";

        public string Catalog => DataCatalog.ReputationDeeds;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema deed = builder.Record(typeof(ReputationDeedEntry)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = deed }],
                // A deed is never read: what reaches the player is the faction and the level it moved to,
                // both worded by their own enum members.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
