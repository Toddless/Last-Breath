namespace Core.Data.Schema
{
    using System;
    using GameData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Npc catalog around its records: one array under a key, one file, and a
    /// name in the localization keyed by the record's own id. What a record is made of is read off
    /// <see cref="NpcData.NpcData"/> and stated nowhere here.</summary>
    /// <remarks>The lifecycle section takes no variants: both cycles are written into one DTO, so the
    /// tool draws its "kind" as a choice of names rather than as a picker of shapes.</remarks>
    public sealed class NpcCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="NpcData.NpcsData.Npcs"/> is
        /// written as.</summary>
        public const string RecordsKey = "npcs";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="NpcData.NpcData.Id"/> is written as.</summary>
        public const string IdField = "id";

        public string Catalog => DataCatalog.Npc;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema npc = builder.Record(typeof(NpcData.NpcData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = npc }],
                // A name and nothing else: an npc has no description key of its own.
                [LocalizedKeyAttribute.NoSuffix],
                new SingleFilePlacement { FileName = DataCatalog.Npc });
        }
    }
}
