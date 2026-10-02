namespace LastBreathTest.Ai
{
    using System.Collections.Generic;
    using Core.Ai.World.Spawn;
    using Core.Enums;
    using Core.Narrative.Facts;
    using Moq;

    [TestClass]
    public class BossRespawnTrackerTests
    {
        private const string BossId = "Npc_Boss_Bone_Pack_Leader";
        private static readonly string CounterKey = FactKeys.BossRespawnDeaths(BossId);

        [TestMethod]
        public void FactionDeathsFeedTheCounterUntilTheThreshold()
        {
            var (facts, counters) = CreateFacts();
            var tracker = new BossRespawnTracker(facts.Object, BossId, Fractions.Undead, deathsPerRespawn: 3);

            tracker.OnDeath(Fractions.Undead, isSummon: false);
            tracker.OnDeath(Fractions.Undead, isSummon: false);
            Assert.IsFalse(tracker.IsRespawnDue);

            tracker.OnDeath(Fractions.Undead, isSummon: false);
            Assert.IsTrue(tracker.IsRespawnDue);

            tracker.ConsumeRespawn();
            Assert.AreEqual(0, counters[CounterKey]);
            Assert.IsFalse(tracker.IsRespawnDue);
        }

        [TestMethod]
        public void LivingBossFreezesTheCounterAndSuppressesRespawn()
        {
            var (facts, counters) = CreateFacts();
            var tracker = new BossRespawnTracker(facts.Object, BossId, Fractions.Undead, deathsPerRespawn: 1)
            {
                BossAlive = true
            };

            tracker.OnDeath(Fractions.Undead, isSummon: false);
            Assert.IsFalse(counters.ContainsKey(CounterKey), "deaths while the boss walks are not banked");
            Assert.IsFalse(tracker.IsRespawnDue);

            // Even a banked-full counter must not fire while he lives.
            counters[CounterKey] = 5;
            Assert.IsFalse(tracker.IsRespawnDue);

            tracker.BossAlive = false;
            Assert.IsTrue(tracker.IsRespawnDue);
        }

        [TestMethod]
        public void ForeignFactionsAndSummonsNeverFeedTheCounter()
        {
            var (facts, counters) = CreateFacts();
            var tracker = new BossRespawnTracker(facts.Object, BossId, Fractions.Undead, deathsPerRespawn: 1);

            tracker.OnDeath(Fractions.Human, isSummon: false);
            tracker.OnDeath(Fractions.Undead, isSummon: true); // bone wolves must not farm their own leader back
            Assert.IsFalse(counters.ContainsKey(CounterKey));
            Assert.IsFalse(tracker.IsRespawnDue);
        }

        private static (Mock<IWorldFactsService> Facts, Dictionary<string, int> Counters) CreateFacts()
        {
            var counters = new Dictionary<string, int>();
            var facts = new Mock<IWorldFactsService>();
            facts.Setup(f => f.GetCount(It.IsAny<string>()))
                .Returns((string key) => counters.GetValueOrDefault(key));
            facts.Setup(f => f.Add(It.IsAny<string>(), It.IsAny<int>()))
                .Callback((string key, int amount) => counters[key] = counters.GetValueOrDefault(key) + amount);
            facts.Setup(f => f.SetCount(It.IsAny<string>(), It.IsAny<int>()))
                .Callback((string key, int value) => counters[key] = value);
            return (facts, counters);
        }
    }
}
