namespace LastBreathTest.Ai
{
    using Core.Ai.World;
    using Core.Entity.Components;

    /// <summary>
    /// The peaceful resident cycle: a knocked-down villager lies for a rolled delay and gets up ALIVE.
    /// The settlement keeps its people, so nothing here may burn a body or end it for good.
    /// </summary>
    [TestClass]
    public class VillagerLifecycleTests
    {
        // Deliberately unlike the config defaults: a cycle that ignored what it was handed and rolled
        // on its own defaults would still look green otherwise.
        private const float ConfiguredMinSeconds = 45f;
        private const float ConfiguredMaxSeconds = 210f;

        private const float ShortestRoll = 0f;
        private const float MiddleRoll = 0.5f;
        private const float LongestRoll = 1f;

        private const float TickStep = 1f;
        private const float LongAfterAnyRecovery = 10000f;

        private const float SavedDelay = 120f;
        private const float SavedElapsed = 90f;

        private const float Tolerance = 0.0001f;

        [TestMethod]
        public void DefeatLaysTheBodyDownAndRollsRecoveryFromConfig()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage); // precondition: nothing has happened yet
            Assert.AreEqual(0, rnd.RangeRolls);

            lifecycle.OnDefeated(isUndead: false);

            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage);
            Assert.AreEqual(1, rnd.RangeRolls, "going down rolls the recovery exactly once");
            Assert.AreEqual(ConfiguredMinSeconds, rnd.AskedMin, Tolerance, "the roll is bounded by the configured recovery range");
            Assert.AreEqual(ConfiguredMaxSeconds, rnd.AskedMax, Tolerance);
            Assert.AreEqual(RolledDelay(MiddleRoll), lifecycle.ResurrectDelay, Tolerance, "the rolled delay is what the save system reads");
            Assert.AreEqual(0f, lifecycle.Elapsed, Tolerance, "a fresh countdown starts from zero");
        }

        [TestMethod]
        public void RolledRecoveryNeverLeavesTheConfiguredRange()
        {
            foreach (float fraction in new[] { ShortestRoll, MiddleRoll, LongestRoll })
            {
                var rnd = new RangeRandom(fraction);
                var lifecycle = CreateLifecycle(rnd);

                lifecycle.OnDefeated(isUndead: false);

                Assert.AreEqual(ConfiguredMinSeconds, rnd.AskedMin, Tolerance);
                Assert.AreEqual(ConfiguredMaxSeconds, rnd.AskedMax, Tolerance);
                Assert.AreEqual(RolledDelay(fraction), lifecycle.ResurrectDelay, Tolerance, $"the cycle keeps the answer of a {fraction} roll verbatim");
                Assert.IsTrue(lifecycle.ResurrectDelay is >= ConfiguredMinSeconds and <= ConfiguredMaxSeconds);
            }
        }

        [TestMethod]
        public void BodyGetsUpAliveExactlyWhenTheRolledDelayIsSpent()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            int rises = 0;
            NpcLifeStage? stageWhenAnnounced = null;
            lifecycle.ReviveReady += () =>
            {
                rises++;
                stageWhenAnnounced = lifecycle.Stage;
            };

            lifecycle.OnDefeated(isUndead: false);
            lifecycle.Tick(RolledDelay(MiddleRoll) - TickStep);

            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage, "one tick short of the delay the body still lies");
            Assert.AreEqual(0, rises);
            Assert.AreEqual(RolledDelay(MiddleRoll) - TickStep, lifecycle.Elapsed, Tolerance, "ticked seconds are counted for the save system");

            lifecycle.Tick(TickStep);

            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage);
            Assert.AreEqual(1, rises, "the rising is announced once, on the tick that spends the delay");
            Assert.AreEqual(NpcLifeStage.Alive, stageWhenAnnounced, "the body already stands when the rising is announced: a handler reads the stage, not a promise of it");
        }

        [TestMethod]
        public void RisingIsAnnouncedOnlyOnceHoweverLongTheBodyIsTicked()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            int rises = 0;
            lifecycle.ReviveReady += () => rises++;

            lifecycle.OnDefeated(isUndead: false);
            lifecycle.Tick(LongAfterAnyRecovery);
            Assert.AreEqual(1, rises); // precondition: the body is already up

            lifecycle.Tick(LongAfterAnyRecovery);
            lifecycle.Tick(TickStep);

            Assert.AreEqual(1, rises, "a standing resident cannot get up again");
            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage);
        }

        [TestMethod]
        public void ResidentCycleRisesAliveAndHasNoUndeadRising()
        {
            VillagerLifecycle lifecycle = CreateLifecycle(new RangeRandom(MiddleRoll));

            // Binds statically: a consumer holding this cycle reaches the alive rising without a cast,
            // and the undead rising is not reachable from it at all — the compiler is the first assert.
            IAliveRiseLifecycle risesAlive = lifecycle;
            INpcLifecycle body = risesAlive;

            Assert.IsInstanceOfType<IAliveRiseLifecycle>(lifecycle, "a resident is the cycle that gets its people back on their feet");
            Assert.IsNotInstanceOfType<IUndeadRiseLifecycle>(lifecycle, "a resident never rises as undead, so nobody may await that outcome from this cycle");
            Assert.AreEqual(NpcLifeStage.Alive, body.Stage);
        }

        [TestMethod]
        public void LivingResidentCannotBeBurned()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage); // precondition: refusal is measured on a standing body

            Assert.IsFalse(lifecycle.CanBeBurned);
            Assert.IsFalse(lifecycle.TryBurn());
            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage, "a refused burning changes no stage");
        }

        [TestMethod]
        public void LyingResidentCannotBeBurnedAndStillGetsUp()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            int rises = 0;
            lifecycle.ReviveReady += () => rises++;

            lifecycle.OnDefeated(isUndead: false);
            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage); // precondition: there IS a body lying to burn

            Assert.IsFalse(lifecycle.CanBeBurned, "no fire keeps a key resident down");
            Assert.IsFalse(lifecycle.TryBurn());
            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage);
            Assert.AreEqual(RolledDelay(MiddleRoll), lifecycle.ResurrectDelay, Tolerance, "a refused burning leaves the countdown alone");

            lifecycle.Tick(RolledDelay(MiddleRoll));

            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage, "the cycle has no final death: the body gets up anyway");
            Assert.AreEqual(1, rises);
        }

        [TestMethod]
        public void RepeatedDefeatDoesNotRerollTheRunningRecovery()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            lifecycle.OnDefeated(isUndead: false);
            lifecycle.Tick(TickStep);
            Assert.AreEqual(1, rnd.RangeRolls); // precondition: exactly one countdown is running

            lifecycle.OnDefeated(isUndead: false);

            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage);
            Assert.AreEqual(1, rnd.RangeRolls, "a body already lying is not knocked down a second time");
            Assert.AreEqual(RolledDelay(MiddleRoll), lifecycle.ResurrectDelay, Tolerance);
            Assert.AreEqual(TickStep, lifecycle.Elapsed, Tolerance, "the running countdown keeps the seconds it already counted");
        }

        [TestMethod]
        public void TickingALivingResidentDoesNothing()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            int rises = 0;
            lifecycle.ReviveReady += () => rises++;
            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage); // precondition: the body was never put down

            lifecycle.Tick(LongAfterAnyRecovery);

            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage);
            Assert.AreEqual(0, rises, "a resident who never fell has nothing to get up from");
            Assert.AreEqual(0, rnd.RangeRolls);
            Assert.AreEqual(0f, lifecycle.ResurrectDelay, Tolerance);
            Assert.AreEqual(0f, lifecycle.Elapsed, Tolerance, "an untouched cycle counts no seconds");
        }

        [TestMethod]
        public void RestoringAnAliveStageIsRefusedAndLeavesTheBodyAsBuilt()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            int rises = 0;
            lifecycle.ReviveReady += () => rises++;

            lifecycle.RestoreState(NpcLifeStage.Alive, SavedDelay, SavedElapsed);

            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage);
            Assert.AreEqual(0f, lifecycle.ResurrectDelay, Tolerance, "a standing body carries no countdown");
            Assert.AreEqual(0f, lifecycle.Elapsed, Tolerance);
            Assert.AreEqual(0, rnd.RangeRolls);

            lifecycle.Tick(LongAfterAnyRecovery);

            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage);
            Assert.AreEqual(0, rises, "a refused restore leaves nothing to tick down");
        }

        [TestMethod]
        public void RestoringDefeatedResumesTheSavedCountdown()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            int rises = 0;
            lifecycle.ReviveReady += () => rises++;

            lifecycle.RestoreState(NpcLifeStage.Defeated, SavedDelay, SavedElapsed);

            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage);
            Assert.AreEqual(SavedDelay, lifecycle.ResurrectDelay, Tolerance);
            Assert.AreEqual(SavedElapsed, lifecycle.Elapsed, Tolerance, "the seconds already lain are counted, not lost");
            Assert.AreEqual(0, rnd.RangeRolls, "a restored countdown is resumed, not rolled anew");

            lifecycle.Tick(SavedDelay - SavedElapsed - TickStep);
            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage);
            Assert.AreEqual(0, rises);

            lifecycle.Tick(TickStep);

            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage, "only the remainder of the saved delay is lain again");
            Assert.AreEqual(1, rises);
        }

        [TestMethod]
        public void DormantAndBurnedSavesLieDownAndRecoverInstead()
        {
            foreach (NpcLifeStage saved in new[] { NpcLifeStage.Dormant, NpcLifeStage.FinalDead })
            {
                var rnd = new RangeRandom(MiddleRoll);
                var lifecycle = CreateLifecycle(rnd);
                int rises = 0;
                lifecycle.ReviveReady += () => rises++;

                lifecycle.RestoreState(saved, SavedDelay, SavedElapsed);

                Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage, $"a {saved} save has no place in a cycle with a single lying stage");
                Assert.AreEqual(SavedDelay, lifecycle.ResurrectDelay, Tolerance);
                Assert.AreEqual(SavedElapsed, lifecycle.Elapsed, Tolerance);
                Assert.AreEqual(0, rnd.RangeRolls);

                lifecycle.Tick(SavedDelay - SavedElapsed);

                Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage, $"a body saved as {saved} gets up instead of lying forever");
                Assert.AreEqual(1, rises);
            }
        }

        [TestMethod]
        public void UndeadFlagCannotChangeTheResidentOutcome()
        {
            var asUndeadRnd = new RangeRandom(MiddleRoll);
            var asUndead = CreateLifecycle(asUndeadRnd);
            var asLivingRnd = new RangeRandom(MiddleRoll);
            var asLiving = CreateLifecycle(asLivingRnd);
            int undeadRises = 0;
            int livingRises = 0;
            asUndead.ReviveReady += () => undeadRises++;
            asLiving.ReviveReady += () => livingRises++;

            asUndead.OnDefeated(isUndead: true);
            asLiving.OnDefeated(isUndead: false);

            Assert.AreEqual(asLiving.Stage, asUndead.Stage, "a resident is never undead, so the flag starts the same lying stage");
            Assert.AreEqual(asLiving.ResurrectDelay, asUndead.ResurrectDelay, Tolerance);
            Assert.AreEqual(asLivingRnd.RangeRolls, asUndeadRnd.RangeRolls);
            Assert.AreEqual(asLivingRnd.AskedMin, asUndeadRnd.AskedMin, Tolerance);
            Assert.AreEqual(asLivingRnd.AskedMax, asUndeadRnd.AskedMax, Tolerance);

            asUndead.Tick(RolledDelay(MiddleRoll));
            asLiving.Tick(RolledDelay(MiddleRoll));

            Assert.AreEqual(NpcLifeStage.Alive, asUndead.Stage, "both flags end in the same rising");
            Assert.AreEqual(livingRises, undeadRises);
            Assert.AreEqual(1, undeadRises);
        }

        [TestMethod]
        public void ResidentKnockedDownAgainRollsAFreshRecovery()
        {
            var rnd = new RangeRandom(MiddleRoll);
            var lifecycle = CreateLifecycle(rnd);
            int rises = 0;
            lifecycle.ReviveReady += () => rises++;

            lifecycle.OnDefeated(isUndead: false);
            lifecycle.Tick(RolledDelay(MiddleRoll));
            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage); // precondition: the resident is back on their feet

            lifecycle.OnDefeated(isUndead: false);

            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage);
            Assert.AreEqual(2, rnd.RangeRolls, "each knock-down rolls its own recovery");
            Assert.AreEqual(0f, lifecycle.Elapsed, Tolerance, "the second countdown starts from zero");

            lifecycle.Tick(RolledDelay(MiddleRoll));

            Assert.AreEqual(NpcLifeStage.Alive, lifecycle.Stage);
            Assert.AreEqual(2, rises, "the settlement keeps its people however often they are knocked down");
        }

        private static VillagerLifecycle CreateLifecycle(RangeRandom rnd) =>
            new(new VillagerLifecycleConfig
            {
                RecoverMinSeconds = ConfiguredMinSeconds,
                RecoverMaxSeconds = ConfiguredMaxSeconds,
            }, rnd);

        /// <summary>What <see cref="RangeRandom"/> answers for the configured range at the given point of it.</summary>
        private static float RolledDelay(float fraction) =>
            ConfiguredMinSeconds + ((ConfiguredMaxSeconds - ConfiguredMinSeconds) * fraction);

        /// <summary>
        /// Answers a range roll at a chosen point of the range and remembers what was asked: which bounds
        /// the cycle rolls between, and how many times it rolls at all, are invisible from the outside otherwise.
        /// </summary>
        private sealed class RangeRandom(float fraction) : IRandomNumberGenerator
        {
            public float AskedMin { get; private set; }

            public float AskedMax { get; private set; }

            public int RangeRolls { get; private set; }

            public float RandFloatRange(float min, float max)
            {
                AskedMin = min;
                AskedMax = max;
                RangeRolls++;
                return min + ((max - min) * fraction);
            }

            public float RandFloat() => fraction;
            public int RandIntRange(int min, int max) => min;
            public float RandFloatN(float mean, float deviation) => mean;
            public uint RandInt() => 0;
            public long RandWeighted(float[] weights) => 0;
            public long RandWeighted(ReadOnlySpan<float> weights) => 0;

            public void Randomize()
            {
            }
        }
    }
}
