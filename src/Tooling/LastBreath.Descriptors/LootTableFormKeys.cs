namespace LastBreath.Descriptors
{
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Tooling.Catalogs.Forms;

    /// <summary>
    /// The words the game writes a loot table with, handed to the form that draws one. The form is
    /// game-free by design and cannot spell them itself; this is the one place they are named, and a test
    /// holds every one of them against what the game's own types and reader write.
    /// </summary>
    public static class LootTableFormKeys
    {
        /// <summary>What a table holds its tiers under — <see cref="LootTableData.Tiers"/>.</summary>
        public const string TiersField = "tiers";

        /// <summary>What a tier is numbered by — <see cref="LootTableTierData.Tier"/>, and, inside a
        /// filter, which tier the augments of the set are of.</summary>
        public const string TierField = "tier";

        /// <summary>What a tier holds its positions under — <see cref="LootTableTierData.Items"/>.</summary>
        public const string ItemsField = "items";

        /// <summary>What a position costs in loot units — <see cref="TableRecord.Price"/>.</summary>
        public const string PriceField = "price";

        /// <summary>Where the augments of a set stand on the rarity scale.</summary>
        public const string RarityField = "rarity";

        /// <summary>The catalog the table form answers for. Named here so that a host drawing it does not
        /// have to know the game's own names for its data.</summary>
        public static string Catalog => DataCatalog.LootTables;

        /// <summary>
        /// The layout of a table for the form drawing one.
        /// <para>Nothing is named for how often a tier comes up: a tier of a table carries no such key —
        /// the chances are one list for the whole game, in the loot configuration — and a key named here
        /// would be a box writing something no reader of the game ever looks at.</para>
        /// </summary>
        public static LootTableLayout Layout => new()
        {
            TiersKey = TiersField,
            TierKey = TierField,
            ItemsKey = ItemsField,
            IdKey = LootTablesCatalogDescriptor.IdField,
            AugmentsKey = LootTablesCatalogDescriptor.AugmentsField,
            PriceKey = PriceField,
            AugmentTierKey = TierField,
            RarityKey = RarityField
        };
    }
}
