namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Data.TradeData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Traders catalog: one array under a key, each shop found by its own id and
    /// named in the localization by it — the shop's window titles itself with that very key. What a trader
    /// is made of is read off <see cref="TraderData"/> and stated nowhere here.</summary>
    /// <remarks>The shelf is a hybrid: the authored half names the goods one by one, the random half names
    /// none and is minted from the equipment templates at roll time. Only the first is a reference, which
    /// is why the rarity weights beside it are a map of rarity to a number and not a list of ids.</remarks>
    public sealed class TradersCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="TradersData.Traders"/> is
        /// written as.</summary>
        public const string RecordsKey = "traders";

        /// <summary>Json name of the field carrying a record's id — what <see cref="TraderData.Id"/> is
        /// written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field naming the faction whose standing prices the shop — what
        /// <see cref="TraderData.Fraction"/> is written as.</summary>
        public const string FractionField = "fraction";

        /// <summary>Json name of the authored half of the shelf — what <see cref="TraderData.Catalog"/> is
        /// written as.</summary>
        public const string ShelfField = "catalog";

        /// <summary>Json name of the field naming one good on that shelf — what
        /// <see cref="TraderCatalogEntryData.ItemId"/> is written as.</summary>
        public const string GoodField = "itemId";

        /// <summary>Json name of the random half of the shelf — what <see cref="TraderData.RandomEquip"/>
        /// is written as.</summary>
        public const string RandomEquipField = "randomEquip";

        /// <summary>Json name of the map of rarity to its weight in the mint roll — what
        /// <see cref="TraderRandomEquipData.RarityWeights"/> is written as.</summary>
        public const string RarityWeightsField = "rarityWeights";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "Traders";

        public string Catalog => DataCatalog.Traders;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema trader = builder.Record(typeof(TraderData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = trader }],
                // A name and nothing else: the trade window titles itself with the trader's own id, and
                // what is for sale reads its description on the thing itself.
                [LocalizedKeyAttribute.NoSuffix],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
