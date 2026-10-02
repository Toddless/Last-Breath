namespace LastBreathTest.Entity
{
    using Core.Data.NpcBuffsData;
    using Core.Entity.NpcModifiers;

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

        /// <summary>An id the catalog never heard of resolves into nothing — and says so through HasBuff.
        /// The two answers must stay separate: "no lines" and "no entry" used to read the same, which is
        /// how a typo'd buff id could leave an NPC unbuffed without a word in the log.</summary>
        [TestMethod]
        public void AnUnknownBuffIdResolvesToNothingAndIsNotHeld()
        {
            var provider = CreateProvider();

            Assert.AreEqual(0, provider.CreateModifiers("Npc_Buff_That_Never_Shipped", "src").Count);
            Assert.AreEqual(0, provider.CreateModifiers("", "src").Count);
            Assert.IsFalse(provider.HasBuff("Npc_Buff_That_Never_Shipped"));
            Assert.IsFalse(provider.HasBuff(string.Empty));
            Assert.IsTrue(provider.HasBuff("Npc_Buff_Double_Health"));
        }

        /// <summary>A buff carrying no lines is still a buff the catalog holds.</summary>
        [TestMethod]
        public void ABuffWithNoLinesIsStillHeld()
        {
            var provider = new NpcBuffProvider(new NpcBuffsData { Buffs = [new NpcBuffData { Id = "Npc_Buff_Empty" }] });

            Assert.IsTrue(provider.HasBuff("Npc_Buff_Empty"));
            Assert.AreEqual(0, provider.CreateModifiers("Npc_Buff_Empty", "src").Count);
            Assert.AreEqual(0, provider.GetGrants("Npc_Buff_Empty").Count);
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
