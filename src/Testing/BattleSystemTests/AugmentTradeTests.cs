namespace LastBreathTest.BattleSystemTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Trade;
    using Moq;

    /// <summary>
    /// An augment as goods. Before it had a price it could be found and carried but never handed over
    /// a counter: the valuation reads an authored base, an augment has none, and an item worth nothing
    /// is refused by both sides of the trade.
    ///
    /// What it is worth is computed from the record rather than written on it — one hundred and
    /// thirty-four authored prices would drift apart from each other and from the loot they drop
    /// beside. Tier and rarity are the two things the record already says, and they are the two things
    /// the price is made of. What the copy ROLLED is deliberately none of it: an augment at the top of
    /// its band is the same augment as one at the bottom, exactly as a blade prices off its blueprint
    /// and not off the numbers its lines happen to carry.
    /// </summary>
    [TestClass]
    public class AugmentTradeTests
    {
        private const string TraderId = "Trader_Test";

        /// <summary>A shipped record whose number is a share, so two draws around it are two values
        /// rather than one rounded count.</summary>
        private const string ShippedRecord = "Augment_Reduce_Cost";

        private const string ShippedProperty = "costShare";

        /// <summary>The band the shipped draws below are taken over. Pinned here rather than read off
        /// the shipped rules, so "two copies rolled differently" stays a claim about the augment.</summary>
        private const float Spread = 0.25f;

        [TestMethod]
        public void TheHigherTierOfTwoAugmentsIsTheDearerOne()
        {
            // Tier is what one augment is worth over another: a socket deep enough to take it is
            // earned, and the price says so without an author pricing every record by hand.
            var valuation = Valuation(Record("Augment_Low", tier: 1), Record("Augment_Mid", tier: 2), Record("Augment_High", tier: 3));

            int low = valuation.Value(Item("Augment_Low", Rarity.Common));
            int mid = valuation.Value(Item("Augment_Mid", Rarity.Common));
            int high = valuation.Value(Item("Augment_High", Rarity.Common));

            Assert.AreEqual(30, low, "tier 1 = base 10 x 3");
            Assert.AreEqual(90, mid, "the tier step compounds");
            Assert.AreEqual(270, high);
            Assert.IsTrue(low < mid && mid < high, "a deeper tier must never be worth less");
        }

        [TestMethod]
        public void TheRarerOfTwoAugmentsOfOneTierIsTheDearerOne()
        {
            // Rarity is the other half of the formula, and it is the same scale every other item is
            // priced on — an augment marked Rare moves with the gear marked Rare.
            var valuation = Valuation(Record("Augment_Plain", tier: 2), Record("Augment_Prize", tier: 2, rarity: Rarity.Rare));

            int plain = valuation.Value(Item("Augment_Plain", Rarity.Common));
            int prize = valuation.Value(Item("Augment_Prize", Rarity.Rare));

            Assert.AreEqual(90, plain);
            Assert.AreEqual(360, prize, "the same tier at the rarity worth four times as much");
            Assert.IsTrue(plain < prize);
        }

        [TestMethod]
        public void TwoCopiesOfOneRecordAtOneRarityAreTheSameAugmentAndCostTheSame()
        {
            // Since the value draw was dropped, two copies of one record at one rarity are identical —
            // so this is no longer a claim about the price ignoring the numbers, it is the claim that
            // the copy is its rarity and nothing else. The rarity IS in the price; that axis is pinned
            // by the case below this one.
            AugmentItemMinter minter = ShippedMinter(seed: 7);
            IAugmentItem first = minter.Mint(ShippedRecord, Rarity.Rare)!;
            IAugmentItem second = minter.Mint(ShippedRecord, Rarity.Rare)!;
            Assert.AreEqual(first.Augment.Values[ShippedProperty], second.Augment.Values[ShippedProperty],
                "two copies of one record at one rarity came out at different numbers");

            var valuation = new ItemValuation(Config(), augments: ShippedAbilityData.Augments());

            Assert.AreEqual(valuation.Value(first), valuation.Value(second),
                "the numbers leaked into the price: two copies of one record at one rarity must cost the same");
            Assert.IsTrue(valuation.Value(first) > 0, "a shipped augment must be worth something at all");
        }

        [TestMethod]
        public void ACopyThatRolledABetterRarityIsWorthMoreThanOneOfTheSameRecordThatDidNot()
        {
            // The consequence of the rarity being drawn per copy rather than authored per record: one
            // record now produces things of different worth, and the shelf has to say so. Priced off
            // the record's own field instead, every copy of an augment would carry one price and the
            // band would be invisible to the player who found the good one.
            var valuation = Valuation(Record("Augment_Banded", tier: 2));

            int plain = valuation.Value(Item("Augment_Banded", Rarity.Common));
            int lucky = valuation.Value(Item("Augment_Banded", Rarity.Rare));

            Assert.IsTrue(lucky > plain, $"two copies of one record priced the same at {plain}, whatever they rolled");
        }

        [TestMethod]
        public void AnAugmentSoldWaitsOnTheBuybackShelfAsTheCopyThatWasSold()
        {
            // The sale as the handler walks it for anything that does not stack: the instance leaves
            // the bag whole, the shelf keeps THAT copy at the price it earned, and buying it back
            // returns the same numbers rather than a fresh draw around the record.
            var catalog = Catalog(Record("Augment_Sold", tier: 2));
            var valuation = new ItemValuation(Config(), augments: catalog);
            var pricing = new TradePricing(valuation, Config());
            var traders = TraderService();
            IAugmentItem sold = Item("Augment_Sold", Rarity.Common);

            int earned = pricing.SellPrice(sold, Fractions.Human);
            Assert.AreEqual(45, earned, "90 of value at half buyback");
            Assert.AreEqual(1, sold.MaxStackSize, "a stacking augment would leave the bag by id and lose its numbers");

            traders.AddBuyback(TraderId, sold, amount: 1, unitPrice: earned);

            var shelf = traders.GetStock(TraderId).Single(offer => offer.IsBuyback);
            Assert.AreEqual(earned, shelf.BuybackUnitPrice, "undoing the sale must cost exactly what it earned");
            var bought = traders.TakeMany(TraderId, shelf.OfferId, 1) as IAugmentItem;
            Assert.IsNotNull(bought, "the shelf handed back something that is not an augment");
            Assert.AreSame(sold.Augment, bought.Augment, "the copy sold was replaced by another one");
        }

        [TestMethod]
        public void AnAugmentOnAShelfIsPricedRatherThanRefused()
        {
            // Nothing stocks augments today, but the valuation is one for both directions: a trader
            // given one has to be able to name a price, or the shelf would carry an item no button
            // can buy.
            var catalog = Catalog(Record("Augment_Offered", tier: 3));
            var pricing = new TradePricing(new ItemValuation(Config(), augments: catalog), Config());

            Assert.AreEqual(270, pricing.BuyPrice(Item("Augment_Offered", Rarity.Common), Fractions.Human));
        }

        [TestMethod]
        public void AnAugmentNoCatalogDeclaresIsUntradable()
        {
            // Same rule as an item whose base nobody authored: nothing says what the augment is, so
            // nothing says what it is worth, and a guessed price is worse than no sale.
            var valuation = Valuation(Record("Augment_Known", tier: 2));

            Assert.AreEqual(0, valuation.Value(Item("Augment_Stranger", Rarity.Legendary)),
                "a record the catalog does not hold must never be priced off its rarity alone");
        }

        private static ItemValuation Valuation(params AbilityAugmentData[] records) =>
            new(Config(), augments: Catalog(records));

        private static IAbilityAugmentCatalog Catalog(params AbilityAugmentData[] records)
        {
            var catalog = new Mock<IAbilityAugmentCatalog>();
            catalog.Setup(source => source.Find(It.IsAny<string>())).Returns((AbilityAugmentData?)null);
            foreach (AbilityAugmentData record in records)
                catalog.Setup(source => source.Find(record.Id)).Returns(record);
            return catalog.Object;
        }

        private static AbilityAugmentData Record(string id, int tier, Rarity rarity = Rarity.Common) =>
            new() { Id = id, Tier = tier, Rarity = rarity };

        private static IAugmentItem Item(string augmentId, Rarity rarity) =>
            new AugmentItem(new AugmentInstance(augmentId, new Dictionary<string, float> { ["share"] = 0.3f }, rarity));

        /// <summary>The formula's own numbers, not the shipped balance: the cases below are about tier
        /// and rarity moving the price, and a balance pass must not turn one of them red.</summary>
        private static ITradeConfigProvider Config()
        {
            var provider = new Mock<ITradeConfigProvider>();
            provider.SetupGet(source => source.Config).Returns(new TradeConfig
            {
                AugmentBasePrice = 10f,
                AugmentTierMultiplier = 3f,
                BuybackFactor = 0.5f,
                RarityMultipliers = new Dictionary<Rarity, float> { [Rarity.Common] = 1f, [Rarity.Rare] = 4f },
            });
            return provider.Object;
        }

        private static TraderService TraderService()
        {
            var traders = new Mock<ITraderProvider>();
            traders.Setup(provider => provider.GetTrader(TraderId)).Returns(
                new TraderDefinition(TraderId, Fractions.Human, RestockGameMinutes: 1440f, Catalog: [], RandomEquip: null));
            return new TraderService(traders.Object, Config());
        }

        /// <summary>The minter the game composes, over the shipped records and a band of a width the
        /// case names.</summary>
        private static AugmentItemMinter ShippedMinter(int seed)
        {
            AbilityAugmentCatalog catalog = ShippedAbilityData.Augments();
            return new AugmentItemMinter(catalog, new AugmentMinter(catalog,
                new DefaultRandomNumberGenerator(seed)));
        }
    }
}
