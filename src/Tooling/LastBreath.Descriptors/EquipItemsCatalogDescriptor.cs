namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.EquipData;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the EquipItems catalog around its records: one array under a key, one file
    /// per slot, and a name and a description in the localization keyed by the record's own id. What a
    /// record is made of is read off <see cref="EquipItemData"/> and stated nowhere here.</summary>
    /// <remarks>A "number or {min,max}" field is read through a converter and comes out of the walk as the
    /// object alone: the contract states shapes as whole records, and a bare number is not one. The tool
    /// draws the scalar form as raw json until it can.</remarks>
    public sealed class EquipItemsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="EquipItemDataList.Items"/> is
        /// written as.</summary>
        public const string RecordsKey = "items";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="EquipItemData.Id"/> is written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field naming the slot an item is worn in, which is also the file it
        /// is written to — what <see cref="EquipItemData.EquipmentPart"/> is written as.</summary>
        public const string SlotField = "equipmentPart";

        public string Catalog => DataCatalog.EquipItems;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema item = builder.Record(typeof(EquipItemData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = item }],
                // A name and a description: an equipment template is read in a tooltip, not only listed.
                [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix],
                new FieldFilePlacement { FieldName = SlotField });
        }
    }
}
