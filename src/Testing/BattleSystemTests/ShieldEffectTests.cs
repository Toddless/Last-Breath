namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source.Effects;

    [TestClass]
    public class ShieldEffectTests
    {
        [TestMethod]
        public void AbsorbsUntilBrokenAndReturnsLeftover()
        {
            var shield = new ShieldEffect(strength: 500);

            Assert.AreEqual(0f, shield.Absorb(300f), "fully absorbed hit leaks nothing");
            Assert.AreEqual(200f, shield.Strength);

            Assert.AreEqual(150f, shield.Absorb(350f), "breaking hit leaks the excess");
            Assert.AreEqual(0f, shield.Strength);

            Assert.AreEqual(100f, shield.Absorb(100f), "broken shield absorbs nothing");
        }
    }
}
