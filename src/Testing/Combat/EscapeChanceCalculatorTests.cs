namespace LastBreathTest.Combat
{
    using Battle.Source;
    using Core.Entity;
    using Core.Enums;
    using Moq;

    [TestClass]
    public class EscapeChanceCalculatorTests
    {
        [TestMethod]
        public void CommonRegularBarelyHoldsAnyone()
        {
            float chance = EscapeChanceCalculator.For([Enemy(EntityType.Regular, Rarity.Common)]);
            Assert.AreEqual(0.95f, chance, 0.001f);
        }

        [TestMethod]
        public void MythicArchonLetsNobodyGo()
        {
            float chance = EscapeChanceCalculator.For([Enemy(EntityType.Archon, Rarity.Mythic)]);
            Assert.AreEqual(0.05f, chance, 0.001f);
        }

        [TestMethod]
        public void ToughestEnemyDictatesTheChance()
        {
            float soloBoss = EscapeChanceCalculator.For([Enemy(EntityType.Boss, Rarity.Legendary)]);
            float bossWithMinion = EscapeChanceCalculator.For([
                Enemy(EntityType.Regular, Rarity.Common),
                Enemy(EntityType.Boss, Rarity.Legendary)
            ]);

            Assert.AreEqual(soloBoss, bossWithMinion, 0.001f);
            Assert.IsTrue(soloBoss < 0.5f);
        }

        [TestMethod]
        public void NoEnemiesMeansFreeExit()
        {
            Assert.AreEqual(0.95f, EscapeChanceCalculator.For([]), 0.001f);
        }

        private static IFightableNpc Enemy(EntityType type, Rarity rarity)
        {
            var npc = new Mock<IFightableNpc>();
            npc.SetupGet(n => n.EntityType).Returns(type);
            npc.SetupGet(n => n.Rarity).Returns(rarity);
            return npc.Object;
        }
    }
}
