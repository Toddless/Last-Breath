namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the CraftingAdditives catalog: one array under a key, each record found by the
    /// resource it speaks for. What an additive does is read off <see cref="CraftingAdditiveEntry"/> and
    /// stated nowhere here.</summary>
    /// <remarks>The record carries no text of its own: it is not an item, it is what one resource does when
    /// it is dropped into an optional slot, and that resource is named in its own catalog.</remarks>
    public sealed class CraftingAdditivesCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="CraftingAdditivesData.Additives"/> is
        /// written as.</summary>
        public const string RecordsKey = "additives";

        /// <summary>Json name of the field a record is found by — what
        /// <see cref="CraftingAdditiveEntry.ResourceId"/> is written as. The resource IS the address: a
        /// second record for one resource is a rule the provider silently overwrites.</summary>
        public const string IdField = "resourceId";

        /// <summary>Json name of the field naming the pool a reforge borrows for one operation — what
        /// <see cref="CraftingAdditiveEntry.RecraftPoolId"/> is written as.</summary>
        public const string PoolField = "recraftPoolId";

        /// <summary>Json name of the field naming the rarity a creation rune guarantees — what
        /// <see cref="CraftingAdditiveEntry.MinRarity"/> is written as.</summary>
        public const string RarityFloorField = "minRarity";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "CraftingAdditives";

        public string Catalog => DataCatalog.CraftingAdditives;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema additive = builder.Record(typeof(CraftingAdditiveEntry)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = additive }],
                // Nothing here reaches the player: the flux in his hand is worded in the resources catalog.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
