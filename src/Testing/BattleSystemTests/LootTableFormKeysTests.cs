namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Enums;
    using LastBreath.Descriptors;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs.Forms;
    using Tooling.Schema;
    using Tooling.Schema.Model;
    using Tooling.Schema.Reflection;

    /// <summary>
    /// The words the loot table form is handed, held against the two readings of a table that already
    /// exist: the game's own reader, which is what the file is written for, and the schema the tool draws
    /// the catalog by, which is what the form walks to find where an id may point.
    /// <para>Nothing else joins them. The form is game-free on purpose, so a key mistyped in the adapter
    /// costs nothing at build time and shows up as a panel writing a price into a key no kill ever
    /// reads.</para>
    /// </summary>
    [TestClass]
    public class LootTableFormKeysTests
    {
        /// <summary>What a table is named by. The form has no business with it — the inspector draws the
        /// row naming a record — so it is spelled here to write the tables the reader is handed.</summary>
        private const string KeyField = "key";

        /// <summary>The word a tier would carry if a table said how often it comes up. It says no such
        /// thing: the chances are one list for the whole game, in the loot configuration. Written out to
        /// hold the layout to that, because the day a tier grows one the form has to be told.</summary>
        private const string ChanceField = "chance";

        private const string TableKey = "basic";
        private const string Drop = "Ring_Onyx";
        private const int TierNumber = 2;
        private const int AugmentTier = 3;
        private const float NamedPrice = 7;
        private const float GroupPrice = 5;

        /// <summary>
        /// A table written with nothing but the layout's own words, read by the game's reader: every seat
        /// arrives, naming what it was written to name and costing what it was written to cost.
        /// <para>Read rather than written, because reading is what the game does with this file: a key the
        /// adapter misspells makes the reader drop the position it stands in — silently, at every kill —
        /// and this is the one place that is noticed.</para>
        /// </summary>
        [TestMethod]
        public void TheLayoutsWordsAreTheOnesTheGamesOwnReaderReadsATableBy()
        {
            LootTableLayout layout = LootTableFormKeys.Layout;

            JObject named = new()
            {
                [layout.IdKey] = Drop,
                [layout.PriceKey] = NamedPrice
            };

            JObject group = new()
            {
                [layout.PriceKey] = GroupPrice,
                [layout.AugmentsKey] = new JObject
                {
                    [layout.AugmentTierKey] = AugmentTier,
                    [layout.RarityKey] = nameof(Rarity.Legendary)
                }
            };

            JObject table = new()
            {
                [KeyField] = TableKey,
                [layout.TiersKey] = new JArray(new JObject
                {
                    [layout.TierKey] = TierNumber,
                    [layout.ItemsKey] = new JArray(named, group)
                })
            };

            LootTableData read = table.ToObject<LootTableData>()
                                 ?? throw new AssertFailedException("a table written with the layout's words was read as nothing at all");

            Assert.AreEqual(TableKey, read.Key);
            Assert.AreEqual(1, read.Tiers.Count, $"'{layout.TiersKey}' is not what the reader takes the tiers of a table from");
            Assert.AreEqual(TierNumber, read.Tiers[0].Tier, $"'{layout.TierKey}' is not what the reader numbers a tier by");
            Assert.AreEqual(2, read.Tiers[0].Items.Count, $"'{layout.ItemsKey}' is not what the reader takes the positions of a tier from");

            TableRecord thing = read.Tiers[0].Items[0];
            Assert.AreEqual(Drop, thing.Id, $"'{layout.IdKey}' is not what a position names one thing under");
            Assert.AreEqual(NamedPrice, thing.Price, $"'{layout.PriceKey}' is not what a position is priced under");
            Assert.IsNull(thing.Augments);

            TableRecord set = read.Tiers[0].Items[1];
            Assert.AreEqual(new AugmentGroup(AugmentTier, Rarity.Legendary), set.Augments,
                $"'{layout.AugmentsKey}', '{layout.AugmentTierKey}' or '{layout.RarityKey}' is not what a set of augments is written with");
            Assert.AreEqual(GroupPrice, set.Price);
        }

        /// <summary>
        /// The same words down the schema the tool draws this catalog by: the form walks it to find where
        /// an id may point and which rarities a set may name, and a key the schema writes under another
        /// name would leave the panel offering a box where the author expects a picker.
        /// </summary>
        [TestMethod]
        public void TheLayoutsWordsAreTheOnesTheDescribedCatalogWritesUnder()
        {
            LootTableLayout layout = LootTableFormKeys.Layout;
            CatalogSchema schema = new CatalogSchemaBuilder(new SchemaReflector()).Build(Descriptor());

            foreach (SectionSchema section in schema.Sections)
            {
                RecordSchema tier = Holding(section.Record, layout.TiersKey);
                RecordSchema position = Holding(tier, layout.ItemsKey);

                Assert.AreEqual(FieldKind.Integer, Field(tier, layout.TierKey).Kind, $"{section.Key}: a tier is numbered by something else");
                Assert.AreEqual(FieldKind.Number, Field(position, layout.PriceKey).Kind, $"{section.Key}: a position is priced by something else");
                FieldSchema id = Field(position, layout.IdKey);
                Assert.AreEqual(FieldKind.Reference, id.Kind, $"{section.Key}: a position names one thing by something else");
                Assert.IsTrue(id.RefTargets.Count > 0, $"{section.Key}: a position names one thing out of nowhere, so the form has nothing to offer");

                RecordSchema group = Holding(position, layout.AugmentsKey);
                Assert.AreEqual(FieldKind.Integer, Field(group, layout.AugmentTierKey).Kind);
                CollectionAssert.AreEqual(Enum.GetNames<Rarity>(), Field(group, layout.RarityKey).EnumValues.ToArray(),
                    $"{section.Key}: the rarities a set of augments may name are not the game's own");

                VariantSet shapes = position.Variants
                                    ?? throw new AssertFailedException($"{section.Key}: the positions take one shape only");

                CollectionAssert.AreEqual(
                    new[] { layout.IdKey, layout.AugmentsKey },
                    shapes.Variants.Select(variant => variant.DiscriminatorValue).ToArray(),
                    $"{section.Key}: a position is told apart by other keys than the layout names");
            }
        }

        /// <summary>
        /// The layout names nothing for how often a tier comes up, because a tier carries no such key: the
        /// chances are one list for the whole game and live in the loot configuration. A key named here
        /// would draw a box writing something no reader of the game ever looks at — and the day a tier
        /// grows one, this is what says the form has to be told about it.
        /// </summary>
        [TestMethod]
        public void TheLayoutNamesNoChanceWhileATierCarriesNone()
        {
            CatalogSchema schema = new CatalogSchemaBuilder(new SchemaReflector()).Build(Descriptor());
            RecordSchema tier = Holding(schema.Sections[0].Record, LootTableFormKeys.Layout.TiersKey);

            Assert.IsFalse(tier.Fields.Any(field => field.JsonName == ChanceField),
                $"a tier writes '{ChanceField}' now: the layout has to name it, or the form draws a table without it");
            Assert.IsNull(LootTableFormKeys.Layout.ChanceKey);
        }

        /// <summary>Which catalog the form answers for, so that a host drawing it does not have to know
        /// the game's own names for its data.</summary>
        [TestMethod]
        public void TheFormAnswersForTheLootTablesCatalog() =>
            Assert.AreEqual(DataCatalog.LootTables, LootTableFormKeys.Catalog);

        private static ICatalogDescriptor Descriptor() =>
            CatalogDescriptors.All.FirstOrDefault(descriptor => descriptor.Catalog == DataCatalog.LootTables)
            ?? throw new AssertFailedException($"{nameof(CatalogDescriptors)} holds no descriptor of the {DataCatalog.LootTables} catalog.");

        private static FieldSchema Field(RecordSchema record, string jsonName) =>
            record.Fields.FirstOrDefault(field => field.JsonName == jsonName)
            ?? throw new AssertFailedException($"'{record.TypeName}' writes no '{jsonName}'.");

        /// <summary>The record a key holds one of, whatever stands between it and them.</summary>
        private static RecordSchema Holding(RecordSchema record, string jsonName)
        {
            FieldSchema field = Field(record, jsonName);

            while (field is { Record: null, Item: { } item }) field = item;

            return field.Record ?? throw new AssertFailedException($"'{record.TypeName}.{jsonName}' holds no records.");
        }
    }
}
