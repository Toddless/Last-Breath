namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.ChestData;
    using Core.Data.GameData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Chests catalog: the file IS the array of records, each chest found by its
    /// own id. What a chest is made of is read off <see cref="ChestData"/> and stated nowhere here.</summary>
    /// <remarks>
    /// The catalog words nothing off a record's id: a chest carries the key it is named under as a field
    /// of its own (<see cref="ChestData.NameKey"/>), so two chests may stand under one name and a renamed
    /// record leaves its wording where it was.
    /// <para>Both modes take one member today — the locks and the generated contents are the design's to
    /// owe — and neither is drawn as a shape of its own: the tool offers the members of an enum, and a
    /// mode grown a second member is a member there and nothing here.</para>
    /// </remarks>
    public sealed class ChestsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>The records sit at the root under no key of their own: the document is the array.</summary>
        public const string RecordsKey = "";

        /// <summary>Json name of the field carrying a record's id — what <see cref="ChestData.Id"/> is
        /// written as.</summary>
        public const string IdField = ChestFields.Id;

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "Chests";

        public string Catalog => DataCatalog.Chests;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema chest = builder.Record(typeof(ChestData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = chest }],
                // Nothing is worded off the id: the name key is a field the record writes for itself.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
