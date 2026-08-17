namespace LastBreathTest.CraftingSystemTests
{
    using System.Globalization;
    using Battle.Source;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Results;
    using Crafting.Source;
    using Crafting.Source.RequestHandlers;
    using LastBreathTest.BattleSystemTests;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The drain the crafting bench did not have. Augments arrive by the handful and most of a handful
    /// is of no use to the build a player is running: nothing took those copies back, so they piled up
    /// in the bag and the only thing to do with one was to leave it there.
    ///
    /// What the drain costs is walked here. Three copies of one class buy one of the SAME class — a
    /// record none of them named, rolled from scratch — so the trade shuffles and never improves, and
    /// a good copy handed in is simply lost. That is the whole design: were the result even a little
    /// better than what went in, this would be the cheapest road to a strong augment rather than the
    /// place to put weak ones.
    ///
    /// And it must never cost the player anything it did not give: three copies whose numbers were
    /// drawn once and can never be drawn again leave the bag ONLY once the thing they bought is
    /// already in it.
    /// </summary>
    [TestClass]
    public class AugmentConversionTests
    {
        /// <summary>The three the cases hand over. Of one class, and worth far more than anything the
        /// conversion gives back — so a result carrying any of their numbers shows at a glance.</summary>
        private const string GivenFirst = "Augment_Test_Given_First";
        private const string GivenSecond = "Augment_Test_Given_Second";
        private const string GivenThird = "Augment_Test_Given_Third";

        /// <summary>The two records of that class the conversion may draw from.</summary>
        private const string BackFirst = "Augment_Test_Back_First";
        private const string BackSecond = "Augment_Test_Back_Second";

        /// <summary>Of the same class in every way but one — the odd ones out a mixed handful is
        /// built from.</summary>
        private const string OtherTier = "Augment_Test_Other_Tier";
        private const string OtherRarity = "Augment_Test_Other_Rarity";

        /// <summary>A class of exactly three records: hand all three over and the conversion has
        /// nothing left to give back.</summary>
        private const string OnlyFirst = "Augment_Test_Only_First";
        private const string OnlySecond = "Augment_Test_Only_Second";
        private const string OnlyThird = "Augment_Test_Only_Third";

        /// <summary>A class whose alternative is a single record, so what comes back is known and only
        /// its numbers are in question.</summary>
        private const string SoleFirst = "Augment_Test_Sole_First";
        private const string SoleSecond = "Augment_Test_Sole_Second";
        private const string SoleThird = "Augment_Test_Sole_Third";
        private const string SoleBack = "Augment_Test_Sole_Back";

        private const string Property = "duration";

        private const string Stackable = "Crafting_Resource_Coal";

        private const int Tier = 2;
        private const int OddTier = 3;
        private const int BareTier = 5;
        private const int SoleTier = 7;

        /// <summary>What the records handed over are worth, and what the records given back are worth.
        /// An order of magnitude apart on purpose: a result that inherited anything from the three
        /// would land in the wrong band entirely.</summary>
        private const float GivenBase = 100f;
        private const float BackBase = 4.5f;

        /// <summary>The band copies are drawn over. Named here rather than read off the shipped rules:
        /// a balance pass closing it must not turn these walks into claims about a file.</summary>
        private const float Spread = 0.25f;

        private const int BagSlots = 8;
        private const int Seed = 23;

        [TestMethod]
        public async Task ThreeOfOneClassComeBackAsOneOfTheSameClassThatIsNoneOfThem()
        {
            // The whole of the trade. Same tier and same rarity, so nothing is gained by the class;
            // a record none of the three named, so nothing is gained by keeping what was already held.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, GivenThird);

            AugmentConversionResult result = await bench.Convert(given);

            Assert.IsTrue(result.Converted, $"the three bought nothing: {result.Outcome}");
            IAugmentItem produced = bench.Produced(result);
            Assert.AreEqual(Tier, bench.Catalog.Find(produced.Id)?.Tier, "the conversion moved the augment to another tier");
            Assert.AreEqual(Rarity.Common, produced.Rarity, "the conversion moved the augment to another rarity");
            CollectionAssert.DoesNotContain(new[] { GivenFirst, GivenSecond, GivenThird }, produced.Id,
                "the conversion handed back one of the very records it was given");
        }

        [TestMethod]
        public async Task TheThreeAreGoneAndTheOneIsHeldInTheirPlace()
        {
            // Three in, one out — the sink half of it. A conversion that left any of the three behind
            // would be a free augment for whoever pressed the button.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, GivenThird);

            AugmentConversionResult result = await bench.Convert(given);

            Assert.IsTrue(result.Converted, $"the three bought nothing: {result.Outcome}");
            foreach (IAugmentItem spent in given)
                Assert.IsNull(bench.Bag.GetItem<IAugmentItem>(spent.InstanceId), $"'{spent.Id}' is still in the bag");
            Assert.AreEqual(1, bench.Bag.GetContents().Count,
                "the bag holds something besides the one augment the conversion gave back");
            Assert.IsNotNull(bench.Bag.GetItem<IAugmentItem>(result.ProducedItemInstanceId!),
                "the conversion took the three and put nothing in the bag");
        }

        [TestMethod]
        public async Task ThreeCopiesOfOneRecordAreAHandfulJustTheSame()
        {
            // What a player actually has: three of the same useless augment. Nothing about the trade
            // asks for three different records — only for three copies.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenFirst, GivenFirst);

            AugmentConversionResult result = await bench.Convert(given);

            Assert.IsTrue(result.Converted, $"three copies of one record bought nothing: {result.Outcome}");
            Assert.AreNotEqual(GivenFirst, bench.Produced(result).Id, "the conversion handed back the record it was given three of");
        }

        [TestMethod]
        public async Task TheDrawReachesEveryRecordOfTheClassAndNeverOneOfTheThree()
        {
            // The exclusion has to be the rule and not the luck of a seed, and the draw has to reach
            // the whole of the class — a conversion locked onto one record would be a recipe.
            var bench = new Bench();
            HashSet<string> produced = [];

            for (int attempt = 0; attempt < 30; attempt++)
            {
                bench.Bag.Clear();
                AugmentConversionResult result = await bench.Convert(bench.Handful(GivenFirst, GivenSecond, GivenThird));
                Assert.IsTrue(result.Converted, $"the three bought nothing: {result.Outcome}");
                produced.Add(bench.Produced(result).Id);
            }

            CollectionAssert.AreEquivalent(new[] { BackFirst, BackSecond }, produced.ToArray(),
                "the draw either misses part of the class or hands back records it was given");
        }

        [TestMethod]
        public async Task TheResultIsMintedAfreshAndCarriesNothingOfWhatWasGiven()
        {
            // The reason the trade is a loss. The result's numbers come out of its OWN record, drawn
            // once for this copy: inherit the three, or come back at a plain base, and the conversion
            // turns into a way to launder weak augments into a strong one.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(SoleFirst, SoleSecond, SoleThird);
            float[] handedIn = [.. given.Select(item => item.Augment.Values[Property])];

            IAugmentItem first = bench.Produced(await bench.Convert(given));
            bench.Bag.Clear();
            IAugmentItem second = bench.Produced(await bench.Convert(bench.Handful(SoleFirst, SoleSecond, SoleThird)));

            Assert.AreEqual(SoleBack, first.Id, "the class holds one alternative and the conversion found another");
            foreach (float rolled in handedIn)
                Assert.IsTrue(rolled > GivenBase / 2f, "the copies handed in are not worth what the case assumes");
            AssertDrawnAroundItsOwnBase(first.Augment.Values[Property], handedIn);
            AssertDrawnAroundItsOwnBase(second.Augment.Values[Property], handedIn);
            Assert.AreEqual(first.Augment.Values[Property], second.Augment.Values[Property],
                "two conversions of one class came out at different numbers — a copy is its rarity and nothing else");
        }

        [TestMethod]
        public async Task AHandfulThatIsNotThreeSeparateCopiesIsRefusedAndCostsNothing()
        {
            // Two is not a handful, four is not a handful, and — the one that would actually be
            // exploited — three ids naming the same copy twice is two augments buying one.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, GivenThird);

            await AssertRefusedForFree(bench, AugmentConversionOutcome.NotThreeAugments, given[0], given[1]);
            await AssertRefusedForFree(bench, AugmentConversionOutcome.NotThreeAugments, given[0], given[1], given[2], given[0]);
            await AssertRefusedForFree(bench, AugmentConversionOutcome.NotThreeAugments,
                given[0].InstanceId, given[1].InstanceId, given[1].InstanceId);
        }

        [TestMethod]
        public async Task AnAugmentTheBagDoesNotHoldIsRefusedAndCostsNothing()
        {
            // A window listing a build the player no longer owns, and the case one step worse: an id
            // the bag does hold, of something that is not an augment at all.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, GivenThird);
            var resource = new CraftingResource(Stackable, 20, [], null!, Rarity.Common);
            Assert.IsTrue(bench.Bag.TryAddItem(resource), "the bag would not take the resource the case is about");

            await AssertRefusedForFree(bench, AugmentConversionOutcome.AugmentNotHeld,
                given[0].InstanceId, given[1].InstanceId, "instance_the_bag_never_held");
            await AssertRefusedForFree(bench, AugmentConversionOutcome.AugmentNotHeld,
                given[0].InstanceId, given[1].InstanceId, resource.InstanceId);
        }

        [TestMethod]
        public async Task AnAugmentNoRecordDeclaresAnyMoreIsRefusedAndCostsNothing()
        {
            // Tier and rarity are written in the record and nowhere else. A copy the catalog has
            // stopped declaring cannot be measured at all, and a conversion that went ahead would
            // trade it into a class it may never have belonged to.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, GivenThird);
            AbilityAugmentCatalog thinned = ShippedAbilityData.CatalogOver(RecordsWithoutTheThird());

            AugmentConversionResult result = await bench.ConvertAgainst(thinned, given);

            Assert.AreEqual(AugmentConversionOutcome.UndeclaredAugment, result.Outcome);
            Assert.AreEqual(3, bench.Bag.GetContents().Count, "the refused conversion spent the copies it could not measure");
        }

        [TestMethod]
        public async Task AHandfulOfMixedTiersIsRefusedAndCostsNothing()
        {
            // Tier is what an augment is worth. Let a handful mix them and the conversion is a ladder:
            // two worthless copies and one good one, traded up.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, OtherTier);

            await AssertRefusedForFree(bench, AugmentConversionOutcome.MixedTiers, given);
        }

        [TestMethod]
        public async Task AHandfulOfMixedRaritiesIsRefusedAndCostsNothing()
        {
            // The same ladder by the other scale. Nothing in the shipped records carries a rarity of
            // its own today, which is exactly why the rule is walked on records that do.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, OtherRarity);

            await AssertRefusedForFree(bench, AugmentConversionOutcome.MixedRarities, given);
        }

        [TestMethod]
        public async Task AClassWithNothingElseInItRefusesOutLoudAndSpendsNothing()
        {
            // Every record of the class is in the player's hands, so there is nothing to draw. The
            // conversion must say so rather than quietly eat three copies for an empty draw — the data
            // can be that thin at any tier, whatever the shipped catalog happens to hold today.
            var bench = new Bench();
            IAugmentItem[] given = bench.Handful(OnlyFirst, OnlySecond, OnlyThird);

            await AssertRefusedForFree(bench, AugmentConversionOutcome.NothingToGiveBack, given);
        }

        [TestMethod]
        public async Task AFullBagKeepsTheThreeInsteadOfSpendingThemOnSomethingWithNowhereToLand()
        {
            // The refusal that has to be safe rather than merely correct. The three were drawn once
            // and cannot be drawn again, so the result goes INTO the bag before they leave it: a bag
            // with no room refuses, and the player still has everything he had.
            var bench = new Bench(slots: 3);
            IAugmentItem[] given = bench.Handful(GivenFirst, GivenSecond, GivenThird);

            await AssertRefusedForFree(bench, AugmentConversionOutcome.NoBagRoom, given);
        }

        [TestMethod]
        public void TheDrainIsAGateTheCompositionAnswers()
        {
            // The window of a later step can do nothing but send the request, and the composition is
            // what answers one. A gate left unregistered is a drain with no way into it.
            IServiceCollection services = new ServiceCollection()
                .AddSharedGameDataParticipants()
                .AddBattleSystemModuleDependencies()
                .AddCraftingSystemModuleDependencies();
            services.AddSingleton<IInventory>(new SlottedBag(BagSlots));
            services.AddSingleton<IRandomNumberGenerator>(new DefaultRandomNumberGenerator(Seed));
            using ServiceProvider container = services.BuildServiceProvider();

            Assert.IsNotNull(container.GetService<IRequestHandler<ConvertAugmentsRequest, AugmentConversionResult>>(),
                "a conversion can be asked for and nothing answers");
        }

        /// <summary>The result landed inside the band of its OWN record and on none of the numbers the
        /// three were worth.</summary>
        private static void AssertDrawnAroundItsOwnBase(float rolled, float[] handedIn)
        {
            Assert.IsTrue(rolled >= BackBase * (1f - Spread) && rolled <= BackBase * (1f + Spread),
                $"the result came out at {rolled}, outside the band its own record is drawn over");
            CollectionAssert.DoesNotContain(handedIn, rolled, "the result came back at a number one of the three was worth");
        }

        private static Task AssertRefusedForFree(Bench bench, AugmentConversionOutcome expected, params IAugmentItem[] offered) =>
            AssertRefusedForFree(bench, expected, [.. offered.Select(item => item.InstanceId)]);

        /// <summary>A refusal that left the bag exactly as it was: the named answer, nothing produced,
        /// and every copy still held at the number it was minted at.</summary>
        private static async Task AssertRefusedForFree(Bench bench, AugmentConversionOutcome expected, params string[] offered)
        {
            IReadOnlyList<(IItem Item, int Amount)> before = bench.Bag.GetContents();
            Dictionary<string, float> rolls = before
                .Select(entry => entry.Item)
                .OfType<IAugmentItem>()
                .ToDictionary(item => item.InstanceId, item => item.Augment.Values[Property]);

            AugmentConversionResult result = await bench.Convert(offered);

            Assert.AreEqual(expected, result.Outcome);
            Assert.IsNull(result.ProducedItemInstanceId, "a refusal produced an augment all the same");
            Assert.AreEqual(before.Count, bench.Bag.GetContents().Count, "the refused conversion changed what the bag holds");
            foreach ((string instanceId, float rolled) in rolls)
            {
                var held = bench.Bag.GetItem<IAugmentItem>(instanceId);
                Assert.IsNotNull(held, "a refused conversion spent one of the copies it was offered");
                Assert.AreEqual(rolled, held.Augment.Values[Property], "a copy came back at another number");
            }
        }

        /// <summary>Four classes of records, read through the game's own loader. Written by the case
        /// rather than taken off the shipped files: every record shipped today is Common, so a walk
        /// over them could not tell the rarity rule from an accident of the data.</summary>
        private static string Records() =>
            $$"""
              {
                  "abilities": [
                      { "id": "Ability_Test_Poison", "tags": [ "{{AbilityTags.Poison}}" ] }
                  ],
                  "augments": [
                      {{Record(GivenFirst, Tier, GivenBase)}},
                      {{Record(GivenSecond, Tier, GivenBase)}},
                      {{Record(GivenThird, Tier, GivenBase)}},
                      {{Record(BackFirst, Tier, BackBase)}},
                      {{Record(BackSecond, Tier, BackBase)}},
                      {{Record(OtherTier, OddTier, GivenBase)}},
                      {{Record(OtherRarity, Tier, GivenBase, Rarity.Uncommon)}},
                      {{Record(OnlyFirst, BareTier, GivenBase)}},
                      {{Record(OnlySecond, BareTier, GivenBase)}},
                      {{Record(OnlyThird, BareTier, GivenBase)}},
                      {{Record(SoleFirst, SoleTier, GivenBase)}},
                      {{Record(SoleSecond, SoleTier, GivenBase)}},
                      {{Record(SoleThird, SoleTier, GivenBase)}},
                      {{Record(SoleBack, SoleTier, BackBase)}}
                  ]
              }
              """;

        /// <summary>The same markup with one record of the offered class dropped — a catalog that has
        /// stopped declaring an augment the player is still carrying.</summary>
        private static string RecordsWithoutTheThird() =>
            $$"""
              {
                  "augments": [
                      {{Record(GivenFirst, Tier, GivenBase)}},
                      {{Record(GivenSecond, Tier, GivenBase)}},
                      {{Record(BackFirst, Tier, BackBase)}}
                  ]
              }
              """;

        /// <summary>One augment record. Numbers are written invariantly: a machine whose culture spells
        /// a fraction with a comma would otherwise hand the loader markup that is not JSON.</summary>
        private static string Record(string id, int tier, float value, Rarity rarity = Rarity.Common) =>
            $$"""
              { "id": "{{id}}", "tier": {{tier}}, "rarity": "{{rarity}}", "tags": [ "{{AbilityTags.Poison}}" ],
                            "upgradeProperties": { "{{Property}}": {{value.ToString(CultureInfo.InvariantCulture)}} } }
              """;

        /// <summary>The records, a bag, the door copies are minted through and the gate a conversion
        /// goes through — the generator shared between mint and draw exactly as the composition shares
        /// its own.</summary>
        private sealed class Bench
        {
            private readonly IRandomNumberGenerator _rnd = new DefaultRandomNumberGenerator(Seed);
            private readonly IAugmentItemMinter _augments;

            internal Bench(int slots = BagSlots)
            {
                Bag = new SlottedBag(slots);
                Catalog = ShippedAbilityData.CatalogOver(Records());
                _augments = new AugmentItemMinter(Catalog,
                    new AugmentMinter(Catalog, _rnd));
            }

            internal SlottedBag Bag { get; }

            internal AbilityAugmentCatalog Catalog { get; }

            /// <summary>Fresh copies of the records named, in the bag and ready to be handed over.</summary>
            internal IAugmentItem[] Handful(params string[] augmentIds) => [.. augmentIds.Select(Held)];

            internal Task<AugmentConversionResult> Convert(IReadOnlyList<IAugmentItem> offered) =>
                ConvertAgainst(Catalog, offered);

            internal Task<AugmentConversionResult> Convert(IReadOnlyList<string> offeredInstanceIds) =>
                new ConvertAugmentsRequestHandler(Bag, Catalog, _augments, _rnd)
                    .HandleRequest(new ConvertAugmentsRequest(offeredInstanceIds));

            /// <summary>The same gate reading another catalog — for the copy whose record left the game.</summary>
            internal Task<AugmentConversionResult> ConvertAgainst(IAbilityAugmentCatalog catalog, IReadOnlyList<IAugmentItem> offered) =>
                new ConvertAugmentsRequestHandler(Bag, catalog, _augments, _rnd)
                    .HandleRequest(new ConvertAugmentsRequest([.. offered.Select(item => item.InstanceId)]));

            /// <summary>The augment a conversion put in the bag, read back the way a window would.</summary>
            internal IAugmentItem Produced(AugmentConversionResult result)
            {
                Assert.IsNotNull(result.ProducedItemInstanceId, "the conversion succeeded and named nothing it produced");
                var produced = Bag.GetItem<IAugmentItem>(result.ProducedItemInstanceId);
                Assert.IsNotNull(produced, "the conversion named an augment the bag does not hold");
                return produced;
            }

            private IAugmentItem Held(string augmentId)
            {
                IAugmentItem? minted = _augments.Mint(augmentId);
                Assert.IsNotNull(minted, $"the records the case wrote declare no '{augmentId}'");
                Assert.IsTrue(Bag.TryAddItem(minted), "the bag would not take a copy the case is about");
                return minted;
            }
        }
    }
}
