namespace LastBreathTest.Modifiers
{
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>Slot capacity per rarity is the affix system's contract with the economy: totals must
    /// stay 1/2/3/4 (the old modifier-amount table) and the odd capacities must split 50/50.</summary>
    [TestClass]
    public class AffixRulesTests
    {
        [TestMethod]
        public void SlotsFor_FixedRarities_YieldExactPairs()
        {
            var rnd = new DefaultRandomNumberGenerator(seed: 1);

            Assert.AreEqual((1, 1), AffixRules.SlotsFor(Rarity.Rare, rnd));
            Assert.AreEqual((2, 2), AffixRules.SlotsFor(Rarity.Legendary, rnd));
            Assert.AreEqual((0, 0), AffixRules.SlotsFor(Rarity.Common, rnd));
            Assert.AreEqual((0, 0), AffixRules.SlotsFor(Rarity.Unique, rnd));
            Assert.AreEqual((0, 0), AffixRules.SlotsFor(Rarity.Mythic, rnd));
        }

        [TestMethod]
        public void SlotsFor_Uncommon_IsOneSlotEitherWayFiftyFifty()
        {
            var rnd = new DefaultRandomNumberGenerator(seed: 20260716);
            int prefixOrientation = 0;
            for (int roll = 0; roll < 10_000; roll++)
            {
                var slots = AffixRules.SlotsFor(Rarity.Uncommon, rnd);
                Assert.IsTrue(slots is (1, 0) or (0, 1), $"Uncommon rolled {slots}.");
                if (slots.Prefixes == 1) prefixOrientation++;
            }

            Assert.IsTrue(prefixOrientation is > 4700 and < 5300,
                $"Uncommon orientation is skewed: {prefixOrientation}/10000 prefix-leaning.");
        }

        [TestMethod]
        public void SlotsFor_Epic_IsThreeSlotsEitherWayFiftyFifty()
        {
            var rnd = new DefaultRandomNumberGenerator(seed: 20260717);
            int prefixOrientation = 0;
            for (int roll = 0; roll < 10_000; roll++)
            {
                var slots = AffixRules.SlotsFor(Rarity.Epic, rnd);
                Assert.IsTrue(slots is (2, 1) or (1, 2), $"Epic rolled {slots}.");
                if (slots.Prefixes == 2) prefixOrientation++;
            }

            Assert.IsTrue(prefixOrientation is > 4700 and < 5300,
                $"Epic orientation is skewed: {prefixOrientation}/10000 prefix-leaning.");
        }
    }
}
