namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the ItemEffects catalog: one array under a key, each record found by the very
    /// passive or effect it hands out. What an entry is made of is read off
    /// <see cref="CraftingEffectEntry"/> and stated nowhere here.</summary>
    /// <remarks>The record carries no text of its own: it is a weight and a payload, and the behaviour it
    /// grants is named and described in the catalog that declares it. The payload's keys belong to the
    /// grant factory — a name the skill does not read hands out nothing — and no catalog holds them.</remarks>
    public sealed class ItemEffectsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="CraftingEffectsData.Effects"/> is
        /// written as.</summary>
        public const string RecordsKey = "effects";

        /// <summary>Json name of the field a record is found by — what
        /// <see cref="CraftingEffectEntry.Id"/> is written as. It is a reference as much as an address: a
        /// second entry for one behaviour would give it two weights in the same roll.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field saying which catalog the id is answered from — what
        /// <see cref="CraftingEffectEntry.Kind"/> is written as.</summary>
        public const string KindField = "kind";

        /// <summary>Json name of the map of factory parameter to its number — what
        /// <see cref="CraftingEffectEntry.Properties"/> is written as.</summary>
        public const string PropertiesField = "properties";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "ItemEffects";

        public string Catalog => DataCatalog.ItemEffects;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema effect = builder.Record(typeof(CraftingEffectEntry)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = effect }],
                // Drawn from, never shown: the grant reads its own name where it is declared.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
