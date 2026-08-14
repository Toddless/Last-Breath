namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Data.LootTable;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Items;
    using Core.MessageBus;
    using Core.Services;
    using LootGeneration.Internal;
    using LootGeneration.Source;
    using Moq;

    /// <summary>
    /// Augments in the loot tables. A table position is picked uniformly among the positions of its
    /// tier, so writing the augments out one by one would decide their share of the drop by counting
    /// them: a hundred and thirty of them beside a dozen pieces of equipment, and equipment stops
    /// dropping. The group is the answer — a position describing a SET by tier and rarity, one seat
    /// at the table however many augments answer it, expanded into a particular augment only when
    /// the drop is minted.
    ///
    /// Walked here: what the form means when read, that the seat is one seat, that the draw stays
    /// inside the filter, that naming a single thing by id still works beside it, and that a set
    /// nobody answers costs the kill one item rather than the kill.
    /// </summary>
    [TestClass]
    public class LootTableAugmentGroupTests
    {
        /// <summary>What every position in these tables costs, and what a kill is given to spend per
        /// item — one seat bought per kill unless a case says otherwise.</summary>
        private const int Seat = 100;

        private const string Equipment = "Equip_Beside_The_Augments";

        private const string OtherEquipment = "Equip_Also_Beside_Them";

        private const string GoodPosition = """{ "id": "Equip_Good", "price": 100 }""";

        // ---------- the form as it is read ----------

        [TestMethod]
        public void AGroupIsReadAsOneSeatCarryingItsFilterAndItsPrice()
        {
            var seat = ParseBasicTier("""[ { "augments": { "tier": 1, "rarity": "Uncommon" }, "price": 120 } ]""").Single();

            Assert.AreEqual(new AugmentGroup(1, Rarity.Uncommon), seat.Augments, "the filter is not what the file describes");
            Assert.AreEqual(120f, seat.Price, "the group's price is the price of the seat, whoever answers it");
            Assert.AreEqual(string.Empty, seat.Id, "a group names a set, never one thing");
            Assert.IsTrue(seat.NamesADrop, "the seat describes a drop and must be worth picking");
        }

        [TestMethod]
        public void APositionNamedByIdIsStillReadTheWayItAlwaysWas()
        {
            var seat = ParseBasicTier("""[ { "id": "Boss_Only_Relic", "price": 640 } ]""").Single();

            Assert.AreEqual("Boss_Only_Relic", seat.Id);
            Assert.AreEqual(640f, seat.Price);
            Assert.IsNull(seat.Augments);
        }

        [TestMethod]
        public void APositionNamingBothAnIdAndAGroupIsRefusedAlone()
        {
            // Two answers to "what drops here" that no reading reconciles: taking either would put a
            // price on a guess.
            var records = ParseBasicTier($$"""
            [
                { "id": "Boss_Only_Relic", "augments": { "tier": 1, "rarity": "Uncommon" }, "price": 300 },
                {{GoodPosition}}
            ]
            """);

            Assert.AreEqual("Equip_Good", records.Single().Id, "the refusal took the whole tier down with it");
        }

        [TestMethod]
        public void APositionNamingNeitherIsRefusedAlone()
        {
            var records = ParseBasicTier($$"""[ { "price": 300 }, {{GoodPosition}} ]""");

            Assert.AreEqual("Equip_Good", records.Single().Id, "a position describing nothing was priced anyway");
        }

        [TestMethod]
        public void AGroupWhoseRarityIsNotOnTheScaleIsRefusedAlone()
        {
            // The strict enum policy of the data layer: a typo must never fall back to the first
            // member of the enum, which here is the best rarity in the game.
            var records = ParseBasicTier($$"""
            [
                { "augments": { "tier": 1, "rarity": "Uncomon" }, "price": 120 },
                {{GoodPosition}}
            ]
            """);

            Assert.AreEqual("Equip_Good", records.Single().Id, "a misspelled rarity was read as some rarity");
        }

        [TestMethod]
        public void AGroupWithoutATierIsRefusedAlone()
        {
            // Left to the default, an unstated tier would quietly mean tier zero — a filter the author
            // never wrote, aimed at the strongest augments there are.
            var records = ParseBasicTier($$"""
            [
                { "augments": { "rarity": "Uncommon" }, "price": 120 },
                {{GoodPosition}}
            ]
            """);

            Assert.AreEqual("Equip_Good", records.Single().Id, "half a filter was accepted as a whole one");
        }

        [TestMethod]
        public void APositionThatCostsNothingIsRefusedAlone()
        {
            // The budget is what makes a table a table: a free seat is bought over and over and the
            // kill's whole item cap goes to it.
            var records = ParseBasicTier($$"""[ { "id": "Equip_Free", "price": 0 }, {{GoodPosition}} ]""");

            Assert.AreEqual("Equip_Good", records.Single().Id, "a position that takes no budget was allowed to take a seat");
        }

        // ---------- one seat, not as many seats as members ----------

        [TestMethod]
        public void AGroupJoinsTheCombinedTableAsOneSeat()
        {
            // The four sources of a kill's table are merged by tier before anything is picked. A group
            // travels through that merge as ONE entry — it is a description, not the things it describes.
            var combined = CombineTable(
                new TableRecord(string.Empty, Seat, new AugmentGroup(1, Rarity.Uncommon)),
                new TableRecord(Equipment, Seat));

            Assert.AreEqual(2, combined[0].Count, "the merged tier holds a number of seats other than the two written");
        }

        [TestMethod]
        public void AGroupIsOneSeatHoweverManyAugmentsAnswerIt()
        {
            // The point of the whole form. Six augments answer the group and two pieces of equipment
            // sit beside it: were the augments seats of their own, they would take six eighths of the
            // drop. As one seat they take a third — a number the designer wrote.
            const int Kills = 600;
            var catalog = Catalog([.. Enumerable.Range(0, 6).Select(index => Augment($"Augment_Member_{index}", 1, Rarity.Uncommon))]);
            var table = Table(
                new TableRecord(string.Empty, Seat, new AugmentGroup(1, Rarity.Uncommon)),
                new TableRecord(Equipment, Seat),
                new TableRecord(OtherEquipment, Seat));

            var drops = DropsOverKills(table, catalog, Kills);

            int augments = drops.Count(id => id.StartsWith("Augment_", StringComparison.Ordinal));
            Assert.AreEqual(Kills, drops.Count, "every kill must afford exactly one seat for the shares to be readable");
            Assert.IsTrue(augments > Kills / 4 && augments < Kills * 3 / 7,
                $"the group took {augments} of {Kills} drops — a third is one seat in three, three quarters is six members in eight");
        }

        // ---------- what the seat expands into ----------

        [TestMethod]
        public void AGroupDropsOnlyTheAugmentsThatAnswerItsFilter()
        {
            // Both halves of the filter are load-bearing: the catalog holds an augment of the right
            // tier at the wrong rarity and one of the right rarity at the wrong tier.
            var catalog = Catalog([
                Augment("Augment_Member_A", 1, Rarity.Uncommon),
                Augment("Augment_Member_B", 1, Rarity.Uncommon),
                Augment("Augment_Wrong_Rarity", 1, Rarity.Rare),
                Augment("Augment_Wrong_Tier", 2, Rarity.Uncommon),
            ]);

            var drops = DropsOverKills(Table(new TableRecord(string.Empty, Seat, new AugmentGroup(1, Rarity.Uncommon))), catalog, kills: 200);

            Assert.AreEqual(200, drops.Count, "the group must drop something on every kill that can afford it");
            CollectionAssert.AreEquivalent(
                new[] { "Augment_Member_A", "Augment_Member_B" },
                drops.Distinct().ToArray(),
                "the draw reached an augment the filter does not describe, or never reached one it does");
        }

        [TestMethod]
        public void ASeatNamedByIdStillDropsExactlyThatThing()
        {
            // The pointed record — one named augment off one named boss — is the other half of the
            // form, and it has to survive the group being added beside it.
            var catalog = Catalog([Augment("Augment_Member_A", 1, Rarity.Uncommon)]);

            var drops = DropsOverKills(Table(new TableRecord("Augment_Member_A", Seat)), catalog, kills: 50);

            Assert.AreEqual(50, drops.Count);
            Assert.IsTrue(drops.All(id => id == "Augment_Member_A"), "a seat naming one thing dropped something else");
        }

        [TestMethod]
        public void AGroupNobodyAnswersNamesNothingToMint()
        {
            // Reported by the draw and answered with nothing. An invented id would be minted into an
            // augment no record declares — a thing that parses, seats and does nothing at all.
            var draw = new TableRecordDraw(Catalog([Augment("Augment_Elsewhere", 2, Rarity.Rare)]), new DefaultRandomNumberGenerator(seed: 1));

            Assert.IsNull(draw.Draw(new TableRecord(string.Empty, Seat, new AugmentGroup(1, Rarity.Uncommon))));
        }

        [TestMethod]
        public void AGroupNobodyAnswersCostsTheKillOneItemAndNotTheKill()
        {
            // The budget for that seat is already spent, so the kill comes up short — but the seats
            // bought beside it still become things, and the drop does not fall over.
            const int Kills = 200;
            var table = Table(
                new TableRecord(string.Empty, Seat, new AugmentGroup(9, Rarity.Legendary)),
                new TableRecord(Equipment, Seat));

            var drops = DropsOverKills(table, Catalog([Augment("Augment_Elsewhere", 2, Rarity.Rare)]), Kills, itemsPerKill: 2);

            Assert.IsTrue(drops.All(id => id == Equipment), "an unanswered group minted something");
            Assert.IsTrue(drops.Count > 0, "the empty group took the whole kill with it");
            Assert.IsTrue(drops.Count < Kills * 2, "nothing was lost, so the empty seat never came up and the case proves nothing");
        }

        [TestMethod]
        public void AGroupDrawsEveryRecordWhoseBandCanReachItsRarityAndNoOther()
        {
            // A record rolls a BAND, so "the Rare augments" is not a list of records stamped Rare —
            // it is every record a Rare copy can come out of. Read as equality the seat would be
            // answered by whatever record happened to be authored without a range, which after the
            // markup is none of them.
            var catalog = Catalog([
                Band("Augment_Spans_It", 1, Rarity.Uncommon, Rarity.Legendary),
                Band("Augment_Ends_At_It", 1, Rarity.Rare, Rarity.Legendary),
                Band("Augment_Stops_Above_It", 1, Rarity.Epic, Rarity.Legendary),
                Band("Augment_Stops_Below_It", 1, Rarity.Uncommon, Rarity.Uncommon),
            ]);

            var drops = DropsOverKills(Table(new TableRecord(string.Empty, Seat, new AugmentGroup(1, Rarity.Rare))), catalog, kills: 300);

            CollectionAssert.AreEquivalent(
                new[] { "Augment_Spans_It", "Augment_Ends_At_It" },
                drops.Distinct().ToArray(),
                "the Rare seat was answered by the wrong records — a band it cannot reach, or a band it can");
        }

        [TestMethod]
        public void AGroupMintsItsMembersAtTheGroupsOwnRarity()
        {
            // The seat is what was bought and priced, so the seat decides what the copy is worth. Left
            // to the record's own draw, an Uncommon seat would keep paying out Legendary augments —
            // every wide-band record can roll one.
            var catalog = Catalog([Band("Augment_Spans_It", 1, Rarity.Uncommon, Rarity.Legendary)]);

            var rarities = ItemsOverKills(Table(new TableRecord(string.Empty, Seat, new AugmentGroup(1, Rarity.Uncommon))), catalog, kills: 200)
                .Select(item => item.Rarity)
                .Distinct()
                .ToList();

            CollectionAssert.AreEquivalent(new[] { Rarity.Uncommon }, rarities,
                "the seat's rarity did not reach the mint: the copies came out at something the seat never bought");
        }

        // The same question asked of the files as they ship — that every group named in a shipped
        // table is answered by an augment — is the loot table audit's
        // (LootTablesAuditTests.EveryLootTableGroupIsAnsweredByAShippedAugment).

        // ---------- the stand ----------

        /// <summary>The ids the drop pipeline actually mints over a run of kills, through the real
        /// service: the table arrives the way a kill asks for it, and only the NPC and the item
        /// factory are stubs.</summary>
        private static List<string> DropsOverKills(
            Dictionary<int, List<TableRecord>> table,
            IAbilityAugmentCatalog catalog,
            int kills,
            int itemsPerKill = 1,
            int seed = 17) =>
            [.. ItemsOverKills(table, catalog, kills, itemsPerKill, seed).Select(item => item.Id)];

        /// <summary>The same run, kept as things rather than ids — for the cases that ask what a drop
        /// turned out to be WORTH.</summary>
        private static List<IItem> ItemsOverKills(
            Dictionary<int, List<TableRecord>> table,
            IAbilityAugmentCatalog catalog,
            int kills,
            int itemsPerKill = 1,
            int seed = 17)
        {
            var rnd = new DefaultRandomNumberGenerator(seed);
            var messageBus = new Mock<IGameMessageBus>();
            messageBus
                .Setup(bus => bus.SendRequest<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(It.IsAny<GetLootTableRequest>()))
                .ReturnsAsync(() => table.ToDictionary(tier => tier.Key, tier => tier.Value.ToList()));

            var service = new LootGenerationService(
                rnd,
                Mock.Of<IGameEventBus>(),
                messageBus.Object,
                ItemFactory(),
                Configuration(itemsPerKill),
                new TableRecordDraw(catalog, rnd));

            List<IItem> dropped = [];
            for (int kill = 0; kill < kills; kill++)
                dropped.AddRange(service.GenerateItemsAsync(Npc().Object).GetAwaiter().GetResult().Select(stack => stack.Item));

            return dropped;
        }

        /// <summary>The union the handler builds out of the four table sources, with everything but
        /// the basic table empty.</summary>
        private static Dictionary<int, List<TableRecord>> CombineTable(params TableRecord[] records)
        {
            var provider = new Mock<ILootTableProvider>();
            provider.SetupGet(tables => tables.BasicTable).Returns([new LootTableTierData { Tier = 0, Items = [.. records] }]);
            provider.Setup(tables => tables.GetLootTable(It.IsAny<Fractions>())).Returns([]);
            provider.Setup(tables => tables.GetLootTable(It.IsAny<EntityType>())).Returns([]);
            provider.Setup(tables => tables.GetLootTable(It.IsAny<string>())).Returns([]);

            return new GetLootTableRequestHandler(provider.Object)
                .HandleRequest(new GetLootTableRequest(Fractions.Undead, EntityType.Regular, "Npc_Any"))
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>One tier holding the given seats — the shape a kill's table has after the merge.</summary>
        private static Dictionary<int, List<TableRecord>> Table(params TableRecord[] records) =>
            new() { [0] = [.. records] };

        /// <summary>A kill affording exactly <paramref name="itemsPerKill"/> seats of one tier, with
        /// nothing rolled around the drop: no rarity spread, no difficulty scaling, no gold.</summary>
        private static ILootConfiguration Configuration(int itemsPerKill)
        {
            var configuration = new Mock<ILootConfiguration>();
            configuration.SetupGet(config => config.TierPrices).Returns([Seat]);
            configuration.SetupGet(config => config.BaseTierChances).Returns([1f]);
            configuration.SetupGet(config => config.BaseRarityChances).Returns([0f, 0f, 0f, 0f, 1f]);
            configuration.SetupGet(config => config.MaxItemsPerKill).Returns(itemsPerKill);
            configuration.SetupGet(config => config.BaseBudget).Returns(new Dictionary<EntityType, float> { [EntityType.Regular] = Seat * itemsPerKill });
            configuration.SetupGet(config => config.RarityMultipliers).Returns(new Dictionary<Rarity, float> { [Rarity.Common] = 1f });
            configuration.SetupGet(config => config.LvlCoefficient).Returns(0f);
            configuration.SetupGet(config => config.ItemModifierMultiplier).Returns(0f);
            configuration.SetupGet(config => config.EquipItemEffectChance).Returns(0f);
            configuration.SetupGet(config => config.GoldPerBudgetUnit).Returns(0f);
            configuration.SetupGet(config => config.MaxGoldPerKill).Returns(0);
            return configuration.Object;
        }

        /// <summary>Hands back a thing named by the id it was asked for, worth what the pipeline said
        /// it is worth: these cases are about WHICH id the table produced and at what rarity, never
        /// about how a kind builds itself.</summary>
        private static IItemCreationService ItemFactory()
        {
            var factory = new Mock<IItemCreationService>();
            factory
                .Setup(service => service.CreateItem(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<Rarity>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<Rarity?>()))
                .Returns((string id, List<string> _, Rarity rolled, float _, float _, Rarity? fixedRarity) =>
                    Mock.Of<IItem>(item => item.Id == id && item.Rarity == (fixedRarity ?? rolled)));
            return factory.Object;
        }

        /// <summary>A fresh kill: a new instance id each time, since the pipeline pays out a given
        /// NPC once per battle.</summary>
        private static Mock<IFightableNpc> Npc()
        {
            var npc = new Mock<IFightableNpc>();
            npc.SetupGet(entity => entity.InstanceId).Returns(Guid.NewGuid().ToString());
            npc.SetupGet(entity => entity.IsSummon).Returns(false);
            npc.SetupGet(entity => entity.Level).Returns(1);
            npc.SetupGet(entity => entity.EntityType).Returns(EntityType.Regular);
            npc.SetupGet(entity => entity.Rarity).Returns(Rarity.Common);
            npc.SetupGet(entity => entity.Fraction).Returns(Fractions.Undead);
            npc.SetupGet(entity => entity.Id).Returns("Npc_Any");
            npc.SetupGet(entity => entity.NpcModifiers).Returns(Mock.Of<INpcModifiersComponent>(modifiers => modifiers.AllModifiers == new List<INpcModifier>()));
            return npc;
        }

        private static IAbilityAugmentCatalog Catalog(AbilityAugmentData[] records) =>
            Mock.Of<IAbilityAugmentCatalog>(catalog => catalog.All == records);

        private static AbilityAugmentData Augment(string id, int tier, Rarity rarity) =>
            new() { Id = id, Tier = tier, Rarity = rarity };

        /// <summary>A record that rolls a range — what every shipped augment is. Worst end first, the
        /// way the field reads.</summary>
        private static AbilityAugmentData Band(string id, int tier, Rarity worst, Rarity best) =>
            new() { Id = id, Tier = tier, MinRarity = worst, MaxRarity = best };

        /// <summary>The basic table's only tier, read out of a whole document the way the game reads
        /// one — the refusals under test happen inside that read.</summary>
        private static List<TableRecord> ParseBasicTier(string items)
        {
            string json = $$"""
            {
                "general": [ { "key": "basic", "tiers": [ { "tier": 0, "items": {{items}} } ] } ],
                "fractions": [],
                "types": [],
                "individual": []
            }
            """;

            return new DataParser(new ItemGameDataFactory()).ParseLootTables(json).BasicTable.Single().Items;
        }
    }
}
