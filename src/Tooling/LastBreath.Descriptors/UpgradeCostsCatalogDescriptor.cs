namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the UpgradeCosts catalog: three sections of one file, one per operation that
    /// is paid for, each holding the price list of one equipment category. What a price line is made of is
    /// read off <see cref="UpgradeRequirementData"/> and stated nowhere here.</summary>
    /// <remarks>A record is found by the category it prices — the same three words in all three sections —
    /// because that is the whole of its address: the operation is the section it stands in, and a second
    /// record for one category is a price list the parser silently overwrites.</remarks>
    public sealed class UpgradeCostsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Section holding what a sharpening attempt costs — what
        /// <see cref="UpgradeCostsData.Upgrade"/> is written as.</summary>
        public const string UpgradeKey = UpgradeCostsData.UpgradeSection;

        /// <summary>Section holding what rerolling one line costs — what
        /// <see cref="UpgradeCostsData.Recraft"/> is written as.</summary>
        public const string RecraftKey = UpgradeCostsData.RecraftSection;

        /// <summary>Section holding what ascension costs — what <see cref="UpgradeCostsData.Ascend"/> is
        /// written as.</summary>
        public const string AscendKey = UpgradeCostsData.AscendSection;

        /// <summary>Json name of the field a price list is found by — what
        /// <see cref="CategoryRequirementsData.Category"/> is written as.</summary>
        public const string IdField = "category";

        /// <summary>Json name of the field holding the lines of one price list — what
        /// <see cref="CategoryRequirementsData.Requirements"/> is written as.</summary>
        public const string RequirementsField = "requirements";

        /// <summary>Json name of the map of item rarity to what it pays instead — what
        /// <see cref="UpgradeRequirementData.ByRarity"/> is written as.</summary>
        public const string ByRarityField = "byRarity";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "UpgradeCosts";

        public string Catalog => DataCatalog.UpgradeCosts;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema prices = builder.Record(typeof(CategoryRequirementsData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.SectionsOfArrays,
                [
                    new SectionSchema { Key = UpgradeKey, Record = prices },
                    new SectionSchema { Key = RecraftKey, Record = prices },
                    new SectionSchema { Key = AscendKey, Record = prices }
                ],
                // A price is spent, never read: what it is spent on is named in the resources catalog.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
