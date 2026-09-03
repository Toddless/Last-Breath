namespace Tooling.Tests.DataEditor
{
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs.Forms;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// A loot table asked the questions its own form asks of one: what stands at which tier, what a seat
    /// written now looks like, what moving one to another tier does to it, and what a tier comes to.
    /// <para>Every gesture here writes a file the game reads and takes budget at every kill, so the
    /// question behind each is the same: is what was written the least the author could have meant, does
    /// one step back take all of it, and is what the file gets wrong shown rather than swallowed.</para>
    /// </summary>
    [TestClass]
    public class LootTableFormTests
    {
        private const string TiersKey = "tiers";
        private const string TierKey = "tier";
        private const string ItemsKey = "items";
        private const string IdKey = "id";
        private const string AugmentsKey = "augments";
        private const string PriceKey = "price";
        private const string RarityKey = "rarity";
        private const string KeyField = "key";

        private const string Basic = "/general/0";
        private const string Other = "/general/1";
        private const string FirstTier = "/general/0/tiers/0";
        private const string SecondTier = "/general/0/tiers/1";
        private const string EmptyTier = "/general/0/tiers/2";
        private const string OtherTier = "/general/1/tiers/0";
        private const string FirstSeat = "/general/0/tiers/0/items/0";
        private const string GroupSeat = "/general/0/tiers/0/items/1";

        private const string Ring = "Ring_Onyx";
        private const string Boots = "Boots_Vital_Core";
        private const string Legendary = "Legendary";
        private const string Rare = "Rare";
        private const string Common = "Common";

        /// <summary>
        /// A table of three tiers: one holding a thing and a set of augments, one holding a thing seated
        /// twice, a seat the game will not buy and a seat naming both of the two things at once, and one
        /// holding no list of seats at all. Beside it a second table, which nothing of the first may be
        /// moved into.
        /// </summary>
        private const string Tables = """
            {
              "general": [
                {
                  "key": "basic",
                  "tiers": [
                    {
                      "tier": 0,
                      "items": [
                        { "id": "Ring_Onyx", "price": 400 },
                        { "price": 600, "augments": { "tier": 3, "rarity": "Legendary" } }
                      ]
                    },
                    {
                      "tier": 1,
                      "items": [
                        { "id": "Ring_Onyx", "price": 120 },
                        { "id": "Ring_Onyx", "price": 130 },
                        { "id": "Boots_Vital_Core", "price": 0 },
                        { "id": "Ring_Onyx", "price": 10, "augments": { "tier": 1, "rarity": "Rare" } }
                      ]
                    },
                    { "tier": 3 }
                  ]
                },
                {
                  "key": "elf",
                  "tiers": [ { "tier": 0, "items": [ { "id": "Boots_Vital_Core", "price": 90 } ] } ]
                }
              ]
            }
            """;

        /// <summary>Every tier of the table and every seat at it, as the file wrote them — including the
        /// tier that holds no list of seats, which is a tier an author may seat something at and not a
        /// tier the form is allowed to pass over.</summary>
        [TestMethod]
        public void Tiers_ReadEveryTierAndEverySeatWhereTheFileWroteThem()
        {
            IReadOnlyList<LootTier> tiers = Read(Document());

            Assert.AreEqual(3, tiers.Count);
            CollectionAssert.AreEqual(new int?[] { 0, 1, 3 }, tiers.Select(tier => tier.Tier).ToArray());
            Assert.AreEqual(At(FirstTier), tiers[0].At);
            Assert.AreEqual(0, tiers[2].Positions.Count, "a tier with no list of seats was read as something other than empty");

            LootPosition named = tiers[0].Positions[0];
            Assert.AreEqual(At(FirstSeat), named.At);
            Assert.AreEqual(Ring, named.Id);
            Assert.IsNull(named.Augments);
            Assert.AreEqual(400d, named.Price);
            Assert.IsTrue(named.NamesOneThing);

            LootPosition group = tiers[0].Positions[1];
            Assert.IsNull(group.Id);
            Assert.AreEqual(3, group.Augments?.Tier);
            Assert.AreEqual(Legendary, group.Augments?.Rarity);
            Assert.AreEqual(600d, group.Price);
            Assert.IsTrue(group.NamesOneThing);
        }

        /// <summary>A seat naming both of the two things a seat may name is not read as either: the game
        /// drops such a position, and a form drawing it as a thing with an id would hide the half of it
        /// nobody meant.</summary>
        [TestMethod]
        public void Tiers_SayThatASeatNamingBothOfTheTwoThingsNamesNeither()
        {
            LootPosition confused = Read(Document())[1].Positions[3];

            Assert.IsFalse(confused.NamesOneThing);
            Assert.AreEqual(Ring, confused.Id, "what the file wrote is still shown: it is what the author has to correct");
            Assert.AreEqual(Rare, confused.Augments?.Rarity);
        }

        /// <summary>What a tier comes to: the seats at it, their price together, and the ones the game
        /// will refuse to buy — a seat priced at nothing is bought over and over for free, so it is
        /// counted where it can still be seen.</summary>
        [TestMethod]
        public void Summary_CountsTheSeatsTheirPriceAndTheOnesTheGameWillNotBuy()
        {
            IReadOnlyList<LootTier> tiers = Read(Document());

            LootTierSummary first = LootTableForm.Summary(tiers[0]);
            Assert.AreEqual(2, first.Positions);
            Assert.AreEqual(1000d, first.Price);
            Assert.AreEqual(0, first.Unpriced);

            LootTierSummary second = LootTableForm.Summary(tiers[1]);
            Assert.AreEqual(4, second.Positions);
            Assert.AreEqual(260d, second.Price);
            Assert.AreEqual(1, second.Unpriced, "a seat priced at nothing was counted as one the game will buy");

            Assert.AreEqual(new LootTierSummary(0, 0, 0), LootTableForm.Summary(tiers[2]));
        }

        /// <summary>A thing seated twice at one tier takes two shares of the draw and nothing else in the
        /// file says so. Named once however many seats it holds: the answer is which ids stand twice.</summary>
        [TestMethod]
        public void Repeated_NamesAnIdSeatedMoreThanOnceExactlyOnce()
        {
            IReadOnlyList<LootTier> tiers = Read(Document());

            CollectionAssert.AreEqual(new[] { Ring }, LootTableForm.Repeated(tiers[1]).ToArray());
            Assert.AreEqual(0, LootTableForm.Repeated(tiers[0]).Count);
        }

        /// <summary>A seat written now is written the way the game's own writer writes one — the thing it
        /// names first, its price after — and one step of the history takes the whole of it back.</summary>
        [TestMethod]
        public void AddPosition_WritesTheSeatTheGamesOwnWriterWouldAndOneStepTakesItBack()
        {
            JsonTreeDocument document = Document();

            Assert.IsTrue(Form().AddPosition(document, At(FirstTier), Boots, 350));

            var seats = (JArray)document.Resolve(At(FirstTier + "/" + ItemsKey))!;
            Assert.AreEqual(3, seats.Count);

            var written = (JObject)seats[2];
            CollectionAssert.AreEqual(new[] { IdKey, PriceKey }, Keys(written));
            Assert.AreEqual(Boots, (string?)written[IdKey]);
            Assert.AreEqual(JTokenType.Integer, written[PriceKey]!.Type, "a price was written as a fraction where the file writes whole numbers");
            Assert.AreEqual(350d, (double)written[PriceKey]!);

            Assert.AreEqual(1, document.History.Depth);
            document.History.Undo();
            Assert.AreEqual(2, ((JArray)document.Resolve(At(FirstTier + "/" + ItemsKey))!).Count);
        }

        /// <summary>A set of augments is written as a filter and never as an id: the two are the shapes a
        /// seat is told apart by, and a set written under an id would be read as one thing that does not
        /// exist.</summary>
        [TestMethod]
        public void AddGroup_WritesTheFilterAndNamesNoId()
        {
            JsonTreeDocument document = Document();

            Assert.IsTrue(Form().AddGroup(document, At(FirstTier), 2, Common, 220));

            var written = (JObject)document.Resolve(At(FirstTier + "/" + ItemsKey + "/2"))!;

            CollectionAssert.AreEqual(new[] { PriceKey, AugmentsKey }, Keys(written));
            Assert.IsFalse(written.ContainsKey(IdKey), "a set of augments was written as a thing named by id");

            var filter = (JObject)written[AugmentsKey]!;
            Assert.AreEqual(2, (int)filter[TierKey]!);
            Assert.AreEqual(Common, (string?)filter[RarityKey]);
            Assert.AreEqual(220d, (double)written[PriceKey]!);
        }

        /// <summary>A tier the file wrote no list of seats for is a tier an author may still seat
        /// something at: the list arrives with the seat, in the one step the gesture is.</summary>
        [TestMethod]
        public void AddPosition_LaysDownTheListOfSeatsWhereTheTierHasNone()
        {
            JsonTreeDocument document = Document();

            Assert.IsTrue(Form().AddPosition(document, At(EmptyTier), Boots, 40));

            var seats = (JArray)document.Resolve(At(EmptyTier + "/" + ItemsKey))!;
            Assert.AreEqual(1, seats.Count);
            Assert.AreEqual(Boots, (string?)seats[0][IdKey]);

            Assert.AreEqual(1, document.History.Depth);
            document.History.Undo();
            Assert.IsNull(document.Resolve(At(EmptyTier + "/" + ItemsKey)), "one step back left the key the gesture laid down");
        }

        /// <summary>
        /// A seat moved to another tier arrives whole: what it names and what it costs travel with it,
        /// because a price says what a thing is worth and not what the tier it stood in is worth.
        /// <para>One step of the history, taken back as one: a table holding the seat twice, or not at
        /// all, is a state no author asked for.</para>
        /// </summary>
        [TestMethod]
        public void MovePosition_CarriesWhatTheSeatNamesAndWhatItCostsInOneStep()
        {
            JsonTreeDocument document = Document();

            Assert.IsTrue(Form().MovePosition(document, At(FirstSeat), At(SecondTier)));

            Assert.AreEqual(1, ((JArray)document.Resolve(At(FirstTier + "/" + ItemsKey))!).Count);

            var seats = (JArray)document.Resolve(At(SecondTier + "/" + ItemsKey))!;
            Assert.AreEqual(5, seats.Count);
            Assert.AreEqual(Ring, (string?)seats[4][IdKey]);
            Assert.AreEqual(400d, (double?)seats[4][PriceKey], "the seat arrived at its new tier without the price it was bought for");

            Assert.AreEqual(1, document.History.Depth, "moving a seat is more than one step of the history");

            document.History.Undo();

            Assert.AreEqual(2, ((JArray)document.Resolve(At(FirstTier + "/" + ItemsKey))!).Count);
            Assert.AreEqual(4, ((JArray)document.Resolve(At(SecondTier + "/" + ItemsKey))!).Count);
            Assert.AreEqual(Ring, (string?)document.Resolve(At(FirstSeat))![IdKey]);
        }

        /// <summary>A set of augments moves the way a thing does — the filter is the seat's, not the
        /// tier's — and the tier it arrives at need not hold a list of seats yet.</summary>
        [TestMethod]
        public void MovePosition_CarriesASetOfAugmentsIntoATierThatHoldsNoSeats()
        {
            JsonTreeDocument document = Document();

            Assert.IsTrue(Form().MovePosition(document, At(GroupSeat), At(EmptyTier)));

            var seats = (JArray)document.Resolve(At(EmptyTier + "/" + ItemsKey))!;
            Assert.AreEqual(1, seats.Count);
            Assert.AreEqual(Legendary, (string?)seats[0][AugmentsKey]?[RarityKey], "the set arrived at its new tier without the filter it names");
            Assert.AreEqual(600d, (double?)seats[0][PriceKey], "the set arrived at its new tier without the price it was bought for");
        }

        /// <summary>The tier it already stands at, a tier of another table, and an address holding
        /// nothing: none of them is a move, and a step filed for any of them would be a press of undo
        /// that takes back nothing.</summary>
        [TestMethod]
        public void MovePosition_RefusesATierOfAnotherTableAndTheOneTheSeatAlreadyStandsAt()
        {
            JsonTreeDocument document = Document();
            LootTableForm form = Form();

            Assert.IsFalse(form.MovePosition(document, At(FirstSeat), At(FirstTier)));
            Assert.IsFalse(form.MovePosition(document, At(FirstSeat), At(OtherTier)), "a seat was moved into another table");
            Assert.IsFalse(form.MovePosition(document, At(FirstTier + "/" + ItemsKey + "/7"), At(SecondTier)));
            Assert.IsFalse(form.MovePosition(document, At(FirstSeat), At(Basic)));

            Assert.AreEqual(0, document.History.Depth);
        }

        /// <summary>A tier arrives numbered and with the empty list of seats it is edited through: a key
        /// the author would have to write before he could add anything to it is a gesture with nothing
        /// behind it.</summary>
        [TestMethod]
        public void AddTier_WritesTheNumberAndTheListOfSeatsItIsFilledThrough()
        {
            JsonTreeDocument document = Document();

            Assert.IsTrue(Form().AddTier(document, At(Basic), 2));

            var written = (JObject)document.Resolve(At(Basic + "/" + TiersKey + "/2"))!;

            CollectionAssert.AreEqual(new[] { TierKey, ItemsKey }, Keys(written));
            Assert.AreEqual(2, (int)written[TierKey]!);
            Assert.AreEqual(0, ((JArray)written[ItemsKey]!).Count);

            Assert.AreEqual(1, document.History.Depth);
        }

        /// <summary>A tier is written where its number puts it — before the first tier standing under a
        /// bigger one. Tier 0 is the best and the list is read as a ladder: a tier laid down at the end
        /// would put the number the author just wrote under the ones it outranks.</summary>
        [TestMethod]
        public void AddTier_WritesTheTierWhereItsNumberPutsItAndNotAtTheEnd()
        {
            JsonTreeDocument document = Document();
            LootTableForm form = Form();

            Assert.IsTrue(form.AddTier(document, At(Basic), 2));
            CollectionAssert.AreEqual(new int?[] { 0, 1, 2, 3 }, Read(document).Select(tier => tier.Tier).ToArray());

            Assert.IsTrue(form.AddTier(document, At(Basic), 4));
            CollectionAssert.AreEqual(new int?[] { 0, 1, 2, 3, 4 }, Read(document).Select(tier => tier.Tier).ToArray(),
                "a tier outranked by every one already written did not go to the end");
        }

        /// <summary>A table the author has only just written holds no list of tiers: the list arrives with
        /// the first tier, in the one step the gesture is.</summary>
        [TestMethod]
        public void AddTier_LaysDownTheListOfTiersWhereTheTableHasNone()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse("""{ "general": [ { "key": "basic" } ] }""");

            Assert.IsTrue(Form().AddTier(document, At(Basic), 0));

            var tiers = (JArray)document.Resolve(At(Basic + "/" + TiersKey))!;

            Assert.AreEqual(1, tiers.Count);
            Assert.AreEqual(0, (int)tiers[0][TierKey]!);
            Assert.AreEqual(1, document.History.Depth);
        }

        /// <summary>The number a tier laid down now carries: the first one the table has not used. A table
        /// merges its tiers by that number, so two tiers under one of them are one tier edited in two
        /// places.</summary>
        [TestMethod]
        public void NextTier_NamesTheFirstNumberTheTableHasNotUsed()
        {
            Assert.AreEqual(2, LootTableForm.NextTier(Read(Document())));
            Assert.AreEqual(0, LootTableForm.NextTier([]));
        }

        /// <summary>A tier taken out goes with the seats standing at it, in one step; so does one seat.</summary>
        [TestMethod]
        public void RemoveTierAndRemovePosition_TakeWhatTheyNameOutInOneStep()
        {
            JsonTreeDocument document = Document();

            Assert.IsTrue(LootTableForm.RemovePosition(document, At(FirstSeat)));
            Assert.AreEqual(1, ((JArray)document.Resolve(At(FirstTier + "/" + ItemsKey))!).Count);

            Assert.IsTrue(LootTableForm.RemoveTier(document, At(FirstTier)));
            Assert.AreEqual(2, ((JArray)document.Resolve(At(Basic + "/" + TiersKey))!).Count);

            Assert.AreEqual(2, document.History.Depth);
        }

        /// <summary>What the schema says about the two values of a seat the form cannot invent: where an
        /// id may point, and which rarities a set of augments may name.</summary>
        [TestMethod]
        public void Describe_ReadsWhereAnIdPointsAndWhichRaritiesASetMayName()
        {
            LootPositionSchema described = Form().Describe(Table());

            CollectionAssert.AreEqual(new[] { ReferenceTarget.Whole("Items") }, described.Drops.ToArray());
            CollectionAssert.AreEqual(new[] { Legendary, Rare, Common }, described.Rarities.ToArray());
        }

        /// <summary>A schema that names none of it — an older build, another catalog — answers with
        /// nothing rather than throwing: what the run cannot describe leaves a box to type into, and the
        /// table is still edited.</summary>
        [TestMethod]
        public void Describe_AnswersWithNothingWhereTheSchemaNamesNoneOfIt()
        {
            LootPositionSchema described = Form().Describe(CatalogFixture.Record(KeyField, CatalogFixture.Field(KeyField, FieldKind.String)));

            Assert.AreEqual(0, described.Drops.Count);
            Assert.AreEqual(0, described.Rarities.Count);
        }

        /// <summary>A record holding no tiers at all — a table the author has only just written — is read
        /// as a table with no tiers and not as a failure.</summary>
        [TestMethod]
        public void Tiers_ReadNothingFromARecordThatHoldsNoTiers()
        {
            JsonTreeDocument document = JsonTreeDocument.Parse("""{ "general": [ { "key": "basic" } ] }""");

            Assert.AreEqual(0, Form().Tiers(document.Resolve(At(Basic)), At(Basic)).Count);
        }

        private static JsonTreeDocument Document() => JsonTreeDocument.Parse(Tables);

        private static JsonPointer At(string pointer) => JsonPointer.Parse(pointer);

        private static IReadOnlyList<LootTier> Read(JsonTreeDocument document) =>
            Form().Tiers(document.Resolve(At(Basic)), At(Basic));

        private static string[] Keys(JObject holder) => [.. holder.Properties().Select(property => property.Name)];

        private static LootTableForm Form() => new(new LootTableLayout
        {
            TiersKey = TiersKey,
            TierKey = TierKey,
            ItemsKey = ItemsKey,
            IdKey = IdKey,
            AugmentsKey = AugmentsKey,
            PriceKey = PriceKey,
            AugmentTierKey = TierKey,
            RarityKey = RarityKey
        });

        /// <summary>The shape of a table as a descriptor states it: tiers holding seats, a seat naming
        /// one thing out of a catalog or a set of augments out of a scale of rarities.</summary>
        private static RecordSchema Table()
        {
            FieldSchema rarity = new()
            {
                JsonName = RarityKey,
                Kind = FieldKind.Enum,
                EnumValues = [Legendary, Rare, Common]
            };

            RecordSchema group = CatalogFixture.Record(null, CatalogFixture.Field(TierKey, FieldKind.Integer), rarity);

            FieldSchema id = new()
            {
                JsonName = IdKey,
                Kind = FieldKind.Reference,
                RefTargets = [ReferenceTarget.Whole("Items")]
            };

            RecordSchema position = CatalogFixture.Record(null, id, CatalogFixture.Field(PriceKey, FieldKind.Number), Holding(AugmentsKey, group));
            RecordSchema tier = CatalogFixture.Record(null, CatalogFixture.Field(TierKey, FieldKind.Integer), ListOf(ItemsKey, position));

            return CatalogFixture.Record(KeyField, CatalogFixture.Field(KeyField, FieldKind.String), ListOf(TiersKey, tier));
        }

        private static FieldSchema Holding(string jsonName, RecordSchema record) =>
            new() { JsonName = jsonName, Kind = FieldKind.Object, Record = record };

        private static FieldSchema ListOf(string jsonName, RecordSchema record) => new()
        {
            JsonName = jsonName,
            Kind = FieldKind.Array,
            Item = Holding(FieldSchema.Unnamed, record)
        };
    }
}
