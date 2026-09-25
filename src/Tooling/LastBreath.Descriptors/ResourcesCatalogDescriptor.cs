namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Resources catalog: one file holding three sections of records — the
    /// material categories, the sharpening resources and the crafting resources — each record found by its
    /// own id and named in the localization by it. What a record is made of is read off the DTOs.</summary>
    /// <remarks>The overlay a category and a material carry — a map of equipment category to the lines
    /// serving only that category — is a map, and the keys of a map are ranked by nothing: they are stated
    /// on the DTO as the members of an enum and carried through as the file had them.</remarks>
    public sealed class ResourcesCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Section of the categories a material belongs to — what
        /// <see cref="ResourcesData.MaterialCategories"/> is written as.</summary>
        /// <remarks>The keys are the game's own: a reference narrowed to one of these sections is written
        /// on a DTO, which cannot see this class, so both sides would spell the word out separately.</remarks>
        public const string MaterialCategoriesKey = ResourcesData.MaterialCategoriesSection;

        /// <summary>Section of the resources sharpening and reforging are paid with — what
        /// <see cref="ResourcesData.UpgradeResources"/> is written as.</summary>
        public const string UpgradeResourcesKey = ResourcesData.UpgradeResourcesSection;

        /// <summary>Section of the resources an item is crafted out of — what
        /// <see cref="ResourcesData.CraftingResources"/> is written as.</summary>
        public const string CraftingResourcesKey = ResourcesData.CraftingResourcesSection;

        /// <summary>Json name of the field carrying a record's id, which every section writes.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field a material writes its own category under — what
        /// <see cref="MaterialData.CategoryId"/> is written as.</summary>
        public const string CategoryField = "categoryId";

        /// <summary>Json name of the map of equipment category to the lines serving only it — what
        /// <see cref="MaterialCategoryData.ByCategory"/> is written as.</summary>
        public const string OverlayField = "byCategory";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two do not even agree, and they are two different facts.</summary>
        public const string FileName = "CraftingResources";

        public string Catalog => DataCatalog.Resources;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return new CatalogSchema(
                RootShape.SectionsOfArrays,
                [
                    Section(MaterialCategoriesKey, builder.Record(typeof(MaterialCategoryData))),
                    Section(UpgradeResourcesKey, builder.Record(typeof(UpgradeResourceData))),
                    Section(CraftingResourcesKey, builder.Record(typeof(CraftingResourceData)))
                ],
                // A name and nothing else: a resource is listed and picked, never read as a paragraph.
                [LocalizedKeyAttribute.NoSuffix],
                new SingleFilePlacement { FileName = FileName });
        }

        /// <summary>The records of one section, all three found by the same field.</summary>
        private static SectionSchema Section(string key, RecordSchema record) =>
            new() { Key = key, Record = record with { IdField = IdField } };
    }
}
