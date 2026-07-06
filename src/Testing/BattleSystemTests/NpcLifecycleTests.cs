namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World;
    using Core.Components;

    [TestClass]
    public class NpcLifecycleTests
    {
        [TestMethod]
        public void DefeatedNonUndeadResurrectsAfterRolledDelay()
        {
            var lifecycle = CreateLifecycle(minSeconds: 10, maxSeconds: 20);
            float? reportedBonus = null;
            lifecycle.ResurrectionReady += bonus => reportedBonus = bonus;

            lifecycle.OnDefeated(isUndead: false);
            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage);

            lifecycle.Tick(9f); // always below the minimum possible delay
            Assert.IsNull(reportedBonus);

            lifecycle.Tick(12f); // 21 total — beyond the maximum possible delay
            Assert.IsNotNull(reportedBonus);
            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage);
        }

        [TestMethod]
        public void ResurrectionBonusScalesWithRolledDelayAndCap()
        {
            var lifecycle = CreateLifecycle(minSeconds: 10, maxSeconds: 20, maxStrengthBonus: 0.5f);
            float? reportedBonus = null;
            lifecycle.ResurrectionReady += bonus => reportedBonus = bonus;

            lifecycle.OnDefeated(isUndead: false);
            lifecycle.Tick(30f);

            Assert.IsNotNull(reportedBonus);
            Assert.AreEqual(lifecycle.StrengthFraction() * 0.5f, reportedBonus.Value, 0.0001f);
            Assert.IsTrue(reportedBonus.Value is >= 0f and <= 0.5f);
        }

        [TestMethod]
        public void DefeatedUndeadGoesDormantAndNeverResurrects()
        {
            var lifecycle = CreateLifecycle();
            bool resurrected = false;
            lifecycle.ResurrectionReady += _ => resurrected = true;

            lifecycle.OnDefeated(isUndead: true);
            lifecycle.Tick(10000f);

            Assert.AreEqual(NpcLifeStage.Dormant, lifecycle.Stage);
            Assert.IsFalse(resurrected);
        }

        [TestMethod]
        public void BurningAFreshBodyPreventsResurrection()
        {
            var lifecycle = CreateLifecycle();
            bool resurrected = false;
            lifecycle.ResurrectionReady += _ => resurrected = true;

            lifecycle.OnDefeated(isUndead: false);
            Assert.IsTrue(lifecycle.TryBurn());
            lifecycle.Tick(10000f);

            Assert.AreEqual(NpcLifeStage.FinalDead, lifecycle.Stage);
            Assert.IsFalse(resurrected);
        }

        [TestMethod]
        public void DormantUndeadCanBeBurned()
        {
            var lifecycle = CreateLifecycle();
            lifecycle.OnDefeated(isUndead: true);

            Assert.IsTrue(lifecycle.CanBeBurned);
            Assert.IsTrue(lifecycle.TryBurn());
            Assert.AreEqual(NpcLifeStage.FinalDead, lifecycle.Stage);
        }

        [TestMethod]
        public void LivingAndFinallyDeadCannotBeBurned()
        {
            var lifecycle = CreateLifecycle();
            Assert.IsFalse(lifecycle.TryBurn()); // alive

            lifecycle.OnDefeated(isUndead: true);
            Assert.IsTrue(lifecycle.TryBurn());
            Assert.IsFalse(lifecycle.TryBurn()); // already burned
        }

        private static NpcLifecycle CreateLifecycle(float minSeconds = 10f, float maxSeconds = 20f, float maxStrengthBonus = 1f) =>
            new(new NpcLifecycleConfig
            {
                ResurrectMinSeconds = minSeconds,
                ResurrectMaxSeconds = maxSeconds,
                MaxStrengthBonus = maxStrengthBonus,
            }, new DefaultRandomNumberGenerator(seed: 42));
    }
}
