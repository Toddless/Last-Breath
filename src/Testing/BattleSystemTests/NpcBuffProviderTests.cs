namespace LastBreathTest.BattleSystemTests
{
    using Battle.Internal.Npc;
    using Core.Data.NpcBuffsData;

    [TestClass]
    public class NpcBuffProviderTests
    {
        [TestMethod]
        public void KnownBuffResolvesIntoModifiersStampedWithSource()
        {
            var provider = CreateProvider();

            var modifiers = provider.CreateModifiers("Npc_Buff_Double_Health", "modifier-instance-1");

            Assert.AreEqual(1, modifiers.Count);
        }

        [TestMethod]
        public void LootOnlyBuffIdResolvesToNothing()
        {
            var provider = CreateProvider();

            Assert.AreEqual(0, provider.CreateModifiers("Npc_Buff_Tier_Upgrade_By_One", "src").Count);
            Assert.AreEqual(0, provider.CreateModifiers("", "src").Count);
        }

        private static NpcBuffProvider CreateProvider() => new(new NpcBuffsData
        {
            Buffs =
            [
                new NpcBuffData
                {
                    Id = "Npc_Buff_Double_Health",
                    Modifiers = [new NpcBuffModifierData { Parameter = "Health", Type = "Increase", Value = 1f }],
                },
            ],
        });
    }
}
