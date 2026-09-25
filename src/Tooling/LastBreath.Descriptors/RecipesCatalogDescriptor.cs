namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Recipes catalog: one array under a key, each record found by its own id
    /// and named in the localization by it. What a recipe is made of is read off
    /// <see cref="CraftingRecipeData"/> and stated nowhere here.</summary>
    /// <remarks>A recipe is a thing in the bag as much as a thing to craft with, so the localization keys
    /// are the item ones — but the name alone: what the recipe makes is described on the equipment template
    /// it names, and a second paragraph here would be the same text twice.</remarks>
    public sealed class RecipesCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="RecipeData.CraftingRecipes"/> is
        /// written as.</summary>
        public const string RecordsKey = "craftingRecipes";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="CraftingRecipeData.Id"/> is written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field naming the equipment template the recipe mints — what
        /// <see cref="CraftingRecipeData.ResultItemId"/> is written as.</summary>
        public const string ResultField = "resultItemId";

        /// <summary>Json name of the field holding the lines a craft is paid with — what
        /// <see cref="CraftingRecipeData.Requirements"/> is written as.</summary>
        public const string RequirementsField = "requirements";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "Recipes";

        public string Catalog => DataCatalog.Recipes;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema recipe = builder.Record(typeof(CraftingRecipeData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = recipe }],
                // A name and nothing else: the recipe is listed and picked, and what it makes reads its
                // own description on the template it names.
                [LocalizedKeyAttribute.NoSuffix],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
