namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.RequestHandlers;
    using Battle.Source.UIElements.PassiveWheel;
    using Core.Battle;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.MessageBus.Requests;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.PassiveTree.Rules;
    using Core.Trade;
    using Moq;

    /// <summary>
    /// Undoing an allocation costs gold. Two systems that know nothing about each other have to settle
    /// together, so what is protected here is that neither can be left half done: a purse short by one
    /// coin gives back no node at all, and a tree that refuses costs nothing.
    /// <para>Not one balance number is written down. Every figure comes out of the pricing the data
    /// catalog feeds, and the assertions are about the SHAPE of the curve — it grows with what the
    /// character earned, it has a ceiling, it is paid once.</para>
    /// </summary>
    [TestClass]
    public class PassiveRespecTests
    {
        private const string Seed = "seed";
        private const string First = "first";
        private const string Second = "second";
        private const string Third = "third";

        [TestMethod]
        public void ARespecChargesGoldAndGivesTheNodesBack()
        {
            Bench bench = Bench.WithChainTaken(gold: 10_000);
            int price = bench.PriceOf(2);
            int before = bench.Wallet.Gold;

            RespecResult result = bench.Respec(Third, Second);

            Assert.IsTrue(result.Refunded, $"the tree refused a legal return: {result.Verdict}");
            Assert.AreEqual(price, result.GoldSpent, "the price charged is not the price the screen could print");
            Assert.AreEqual(before - price, bench.Wallet.Gold);
            Assert.AreEqual(1, bench.Tree.SpentPoints, "the points did not come back");
            Assert.IsFalse(bench.Tree.IsTaken(Second));
            Assert.IsFalse(bench.Tree.IsTaken(Third));
        }

        /// <summary>The reason the set is charged as a set: a partial return would leave the character
        /// without the gold AND without the nodes, and nothing downstream could repair that.</summary>
        [TestMethod]
        public void TooLittleGold_GivesBackNoNodeAtAll()
        {
            Bench bench = Bench.WithChainTaken(gold: 0);
            bench.Wallet.Add(bench.PriceOf(2) - 1);
            int before = bench.Wallet.Gold;

            RespecResult result = bench.Respec(Third, Second);

            Assert.IsFalse(result.Refunded);
            Assert.IsTrue(result.NotEnoughGold, "the refusal did not say which of the two walls the player hit");
            Assert.AreEqual(0, result.GoldSpent);
            Assert.AreEqual(before, bench.Wallet.Gold, "a refused respec still cost money");
            Assert.AreEqual(3, bench.Tree.SpentPoints, "a refused respec gave part of itself back");
            Assert.IsTrue(bench.Tree.IsTaken(Second));
            Assert.IsTrue(bench.Tree.IsTaken(Third));
        }

        /// <summary>An impossible operation costs nothing, the same way a purchase that cannot happen is
        /// never charged for. The tree answers first and the purse is never opened.</summary>
        [TestMethod]
        public void ARefusalFromTheTreeCostsNothing()
        {
            Bench bench = Bench.WithChainTaken(gold: 10_000);
            int before = bench.Wallet.Gold;

            RespecResult stranded = bench.Respec(Second);
            RespecResult nothing = bench.Respec();

            Assert.AreEqual(AllocationResult.WouldOrphan, stranded.Verdict, "the middle of a branch was given back on its own");
            Assert.AreEqual(AllocationResult.NotTaken, nothing.Verdict, "giving nothing back was reported as a respec");
            Assert.IsFalse(stranded.NotEnoughGold, "a rule refusal was reported as an empty purse");
            Assert.AreEqual(before, bench.Wallet.Gold, "a respec the rules refused was charged for");
            Assert.AreEqual(3, bench.Tree.SpentPoints);
        }

        [TestMethod]
        public void ThePriceGrowsWithTheLevelsTheCharacterEarned()
        {
            IPassiveRespecPricing pricing = Pricing();

            Assert.IsTrue(pricing.PriceOf(1, 0) > 0, "a respec is free at level zero");
            Assert.IsTrue(pricing.PriceOf(1, 40) > pricing.PriceOf(1, 0), "the price does not move with the character");
            Assert.IsTrue(pricing.PriceOf(4, 0) > pricing.PriceOf(1, 0), "four nodes cost no more than one");
            Assert.AreEqual(0, pricing.PriceOf(0, 40), "a respec of nothing was priced");
        }

        [TestMethod]
        public void ThePriceHasACeilingFromTheData()
        {
            IPassiveRespecPricing pricing = Pricing();

            Assert.AreEqual(pricing.PriceOf(10_000, 50), pricing.PriceOf(20_000, 50),
                "the price runs away instead of stopping at the ceiling the data sets");
        }

        /// <summary>
        /// The price follows what the character EARNED and not what he is wearing. Read off the effective
        /// level it would fall the moment a ring granting levels came off, which is a respec bought at
        /// whatever the cheapest gear of the moment says.
        /// </summary>
        [TestMethod]
        public void EquipmentLevelsDoNotMoveThePrice()
        {
            Bench bench = Bench.WithChainTaken(gold: 10_000);
            bench.Mastery.AddExperience(100_000);
            int earned = bench.Mastery.EarnedLevel;
            int priceByEarnedLevel = bench.PriceOf(2);

            bench.Mastery.AddBonusLevel();
            bench.Mastery.AddBonusLevel();

            Assert.IsTrue(bench.Mastery.CurrentLevel > earned, "the fixture must actually grant bonus levels");
            Assert.IsTrue(priceByEarnedLevel > bench.PriceOf(2, level: 0), "the fixture must be at a level the price can move with");

            RespecResult result = bench.Respec(Third, Second);

            Assert.IsTrue(result.Refunded);
            Assert.AreEqual(priceByEarnedLevel, result.GoldSpent, "putting a ring on changed what a respec costs");
        }

        /// <summary>Every number in the formula comes out of the catalog. A build with no file behind it
        /// still charges — a respec that quietly became free is the one failure the price exists to
        /// prevent — and the file it is given is what it charges by.</summary>
        [TestMethod]
        public void ThePricingReadsItsNumbersFromTheCatalog()
        {
            var provider = new PassiveTreeRulesProvider();
            CollectionAssert.AreEqual(new[] { DataCatalog.PassiveTreeRules }, provider.Catalogs.ToArray());

            int shipped = provider.PriceOf(4, 0);
            Assert.IsTrue(shipped > 0, "a pricing with no file behind it hands respecs out for free");

            provider.Apply(DataCatalog.PassiveTreeRules, new GameDataFile("PassiveTreeRules.json",
                """{ "respec": { "goldPerNode": 1000, "masteryScale": 0, "maxCost": 999999 } }"""));

            Assert.AreEqual(4000, provider.PriceOf(4, 0), "the catalog did not reach the formula");
            Assert.AreNotEqual(shipped, provider.PriceOf(4, 0));
        }

        /// <summary>
        /// The number the screen prints and the number the gate takes are the same number, and the screen
        /// reaches it through one reading of its own (<see cref="PassiveRespecQuotes"/>). A second
        /// arithmetic anywhere on the way to the button is a price the player agreed to and did not pay.
        /// </summary>
        [TestMethod]
        public void TheScreenQuotesExactlyWhatTheGateCharges()
        {
            Bench bench = Bench.WithChainTaken(gold: 10_000);
            bench.Mastery.AddExperience(20_000);
            var quotes = new PassiveRespecQuotes(Pricing(), bench.Mastery, bench.Wallet);

            RespecQuote quote = quotes.Quote(2);
            Assert.IsTrue(quote.Affordable, "the fixture must be able to afford what it is about to buy");

            RespecResult result = bench.Respec(Third, Second);

            Assert.IsTrue(result.Refunded);
            Assert.AreEqual(quote.Gold, result.GoldSpent, "the button and the till disagree");
        }

        /// <summary>An empty purse is reported by the quote before the click, so the confirmation can be
        /// shut with the reason named instead of being sent to the gate to fail.</summary>
        [TestMethod]
        public void AQuoteSaysWhetherThePurseCoversIt()
        {
            Bench bench = Bench.WithChainTaken(gold: 0);
            var quotes = new PassiveRespecQuotes(Pricing(), bench.Mastery, bench.Wallet);

            Assert.IsFalse(quotes.Quote(2).Affordable);

            bench.Wallet.Add(quotes.Quote(2).Gold);

            Assert.IsTrue(quotes.Quote(2).Affordable);
            Assert.IsFalse(new PassiveRespecQuotes(null, null, null).CanCharge,
                "a build with no purse behind it quoted a price it could charge");
        }

        /// <summary>The shipped file is what the game charges by. Its absence was invisible — the code
        /// defaults matched it — so what is pinned is that the file PARSES and prices the same, which is
        /// the only way a typo in it would ever be noticed.</summary>
        [TestMethod]
        public void TheShippedRulesFileIsReadAndPricesWhatTheCodeDefaultsTo()
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.PassiveTreeRules), "PassiveTreeRules.json");
            Assert.IsTrue(File.Exists(path), $"the shipped passive tree rules are missing at {path}");

            var shipped = new PassiveTreeRulesProvider();
            shipped.Apply(DataCatalog.PassiveTreeRules, new GameDataFile(Path.GetFileName(path), File.ReadAllText(path)));

            IPassiveRespecPricing defaults = Pricing();
            foreach ((int nodes, int level) in new[] { (1, 0), (4, 12), (40, 50), (10_000, 50) })
                Assert.AreEqual(defaults.PriceOf(nodes, level), shipped.PriceOf(nodes, level),
                    $"the shipped file prices {nodes} nodes at level {level} differently from the code it mirrors");
        }

        private static IPassiveRespecPricing Pricing() => new PassiveTreeRulesProvider();

        /// <summary>The three systems a respec settles, wired the way the game wires them.</summary>
        private sealed class Bench
        {
            private readonly RespecPassiveNodesRequestHandler _handler;

            private Bench(int gold)
            {
                Tree = new PassiveTreeService(new TreeProviderStub(Chain()), ConditionCatalogs.Empty());
                Tree.SetTotalPoints(10);
                Mastery = new MartialArtMasteryStand();
                Wallet = new WalletService(NoStartingGold());
                Wallet.Add(gold);

                _handler = new RespecPassiveNodesRequestHandler(Tree, Pricing(), Mastery, Wallet);
            }

            public PassiveTreeService Tree { get; }

            public MartialArtMasteryStand Mastery { get; }

            public WalletService Wallet { get; }

            public static Bench WithChainTaken(int gold)
            {
                var bench = new Bench(gold);
                Assert.AreEqual(AllocationResult.Success, bench.Tree.TakePath(bench.Tree.PathTo(Third)));
                return bench;
            }

            public int PriceOf(int nodes) => PriceOf(nodes, Mastery.EarnedLevel);

            public int PriceOf(int nodes, int level) => Pricing().PriceOf(nodes, level);

            public RespecResult Respec(params string[] nodeIds) =>
                _handler.HandleRequest(new RespecPassiveNodesRequest(nodeIds)).GetAwaiter().GetResult();

            private static PassiveTreeDocument Chain()
            {
                var document = new PassiveTreeDocument();
                document.AddNode(new PassiveNode { Id = Seed, Kind = PassiveNodeKind.Start, Stance = Stance.Strength });
                document.AddNode(new PassiveNode { Id = First, Kind = PassiveNodeKind.Small, X = 10f });
                document.AddNode(new PassiveNode { Id = Second, Kind = PassiveNodeKind.Small, X = 20f });
                document.AddNode(new PassiveNode { Id = Third, Kind = PassiveNodeKind.Small, X = 30f });

                document.Link(Seed, First);
                document.Link(First, Second);
                document.Link(Second, Third);

                return document;
            }

            private static ITradeConfigProvider NoStartingGold()
            {
                var provider = new Mock<ITradeConfigProvider>();
                provider.SetupGet(config => config.Config).Returns(new TradeConfig());
                return provider.Object;
            }
        }

        /// <summary>
        /// Mastery reduced to the two levels a price cares about. The real one is a data participant with
        /// a curve behind it, and what is under test is which of its two levels the price reads — so the
        /// stand lets both be moved independently, which the real class deliberately does not.
        /// </summary>
        private sealed class MartialArtMasteryStand : IMartialArtMastery
        {
            public int EarnedLevel { get; private set; }

            public int BonusLevel { get; private set; }

            public int BonusPoints { get; private set; }

            public int CurrentLevel => EarnedLevel + BonusLevel;

            public int TotalPoints => EarnedLevel + BonusPoints;

            public int MaximumLevel => 50;

            public int CurrentExperience { get; private set; }

            public string Id => "Mastery_Martial_Art";

            public string InstanceId { get; } = Guid.NewGuid().ToString();

            public Godot.Texture2D? Icon => null;

            public string Description => string.Empty;

            public string DisplayName => string.Empty;

            public event Action<int>? BonusLevelChange, CurrentLevelChange, ExperienceChange;

            public void AddExperience(int experience)
            {
                CurrentExperience += experience;
                EarnedLevel = Math.Min(MaximumLevel, EarnedLevel + experience / 1000);
                CurrentLevelChange?.Invoke(CurrentLevel);
                ExperienceChange?.Invoke(CurrentExperience);
            }

            public void AddBonusLevel()
            {
                BonusLevel++;
                BonusLevelChange?.Invoke(BonusLevel);
            }

            public void RemoveBonusLevel() => BonusLevel--;

            public void AddBonusPoints(int points) => BonusPoints += points;

            public int ExpToNextLevelRemain() => 0;

            public int ExpToNextLevelTotal() => 0;

            public bool IsSame(string otherId) => InstanceId.Equals(otherId, StringComparison.Ordinal);

            public void RestoreState(int baseLevel, int experience, int bonusPoints)
            {
                EarnedLevel = baseLevel;
                BonusPoints = bonusPoints;
            }
        }

        private sealed class TreeProviderStub(PassiveTreeDocument tree) : IPassiveTreeProvider
        {
            public PassiveTreeDocument Tree { get; } = tree;

            public IReadOnlyList<string> Issues => [];
        }
    }
}
