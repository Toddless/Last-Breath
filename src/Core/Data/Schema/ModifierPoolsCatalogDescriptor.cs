namespace Core.Data.Schema
{
    using System;
    using EquipData;
    using GameData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the modifier pools around their records: one array under a key and no
    /// author-facing text at all. What a pool is made of is read off <see cref="EquipModifierPoolData"/>
    /// and stated nowhere here.</summary>
    /// <remarks>The catalog ships two files with two meanings — the pools a rolled item draws its lines
    /// from, and the mythic pools the ascension gift draws from — and nothing on a POOL says which of them
    /// it belongs to: the mythic mark is written on its entries, one level below the record a placement
    /// rule is handed. So the file a new pool is written to is left to the tool.</remarks>
    public sealed class ModifierPoolsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="EquipModifiersPoolRoot.Root"/> is
        /// written as.</summary>
        public const string RecordsKey = "pools";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="EquipModifierPoolData.Id"/> is written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field holding the entries a roll draws from — what
        /// <see cref="EquipModifierPoolData.ModifiersPool"/> is written as.</summary>
        public const string EntriesField = "modifiersPool";

        public string Catalog => DataCatalog.ModifierPools;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema pool = builder.Record(typeof(EquipModifierPoolData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = pool }],
                // A pool is drawn from, never shown: the line it hands out is worded from its own parameter.
                [],
                new FreeFilePlacement());
        }
    }
}
