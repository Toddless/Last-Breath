namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World;
    using Core.Ai.World.Activities;
    using Core.Ai.World.Recovery;
    using Core.Ai.World.Time;
    using Core.Entity.Components;
    using Godot;

    [TestClass]
    public class WorldActivitiesTests
    {
        // The recovery gate's settings in these tests; the health shares below are read off the threshold.
        private const float RetreatThreshold = 0.5f;
        private const float GiveUpMinutes = 2f;
        private const float BelowThreshold = RetreatThreshold / 2f;
        private const float AboveThreshold = (RetreatThreshold + 1f) / 2f; // past the threshold, short of full

        // ---- CycleActivity + TimedTask ----

        [TestMethod]
        public void CycleAdvancesWhenTheTaskCompletesAndLoops()
        {
            var brain = CreateBrain(out _);
            var first = new RecordingTask();
            var second = new RecordingTask();
            var cycle = new CycleActivity([first, second]);

            cycle.Enter(brain);
            Assert.AreEqual(1, first.Entered);

            first.Complete();
            cycle.Tick(brain, 0.1f); // advance: first exits+restarts, second enters
            Assert.AreEqual(1, first.Exited);
            Assert.IsFalse(first.IsCompleted, "advancing must restart the finished task for its next lap");
            Assert.AreEqual(1, second.Entered);

            second.Complete();
            cycle.Tick(brain, 0.1f); // loop back to the first
            Assert.AreEqual(2, first.Entered);
        }

        [TestMethod]
        public void CycleResumesTheCurrentStepAcrossInterruptions()
        {
            var brain = CreateBrain(out _);
            var first = new RecordingTask();
            var second = new RecordingTask();
            var cycle = new CycleActivity([first, second]);

            cycle.Enter(brain);
            first.Complete();
            cycle.Tick(brain, 0.1f); // now on the second step

            cycle.Exit(brain);  // battle interruption
            cycle.Enter(brain); // back to calm

            Assert.AreEqual(2, second.Entered, "the cycle must resume the same step, not start over");
            Assert.AreEqual(1, first.Entered);
        }

        [TestMethod]
        public void TimedTaskDoesNotCountTheInterruptionGap()
        {
            var brain = CreateBrain(out _);
            var clock = new SettableClock { MinuteOfDay = 100 };
            var task = new TimedTask(new NoopActivity(), minutes: 10, clock);

            task.Enter(brain);
            clock.MinuteOfDay = 105;
            task.Tick(brain, 0.1f); // 5 minutes of work
            Assert.IsFalse(task.IsCompleted);

            task.Exit(brain);            // interruption (battle)
            clock.MinuteOfDay = 300;     // a long fight
            task.Enter(brain);           // resume: the gap must not count as work
            task.Tick(brain, 0.1f);
            Assert.IsFalse(task.IsCompleted);

            clock.MinuteOfDay = 306;     // 5 done before + 6 now >= 10
            task.Tick(brain, 0.1f);
            Assert.IsTrue(task.IsCompleted);
        }

        // ---- RecoveryGateActivity ----

        [TestMethod]
        public void WoundedNpcAbandonsTheRoutineAndRestsUntilFull()
        {
            var brain = CreateBrain(out var agent);
            var inner = new RecordingTask();
            var gate = CreateGate(inner);

            gate.Enter(brain);
            Assert.AreEqual(1, inner.Entered);

            agent.HealthPercent = BelowThreshold;
            gate.Tick(brain, 0.1f);
            Assert.AreEqual(1, inner.Exited, "the routine is abandoned for recovery");

            agent.HealthPercent = AboveThreshold; // the zone heals — rising, no stall
            gate.Tick(brain, 0.1f);
            Assert.AreEqual(1, inner.Entered, "still recovering below full");

            agent.HealthPercent = 1f;
            gate.Tick(brain, 0.1f);
            Assert.AreEqual(2, inner.Entered, "full health returns the NPC to its routine");
        }

        [TestMethod]
        public void RecoveryEndsAfterAStallGiveUpAndRearmsOnlyPastTheThreshold()
        {
            var brain = CreateBrain(out var agent);
            var inner = new RecordingTask();
            var gate = CreateGate(inner);

            gate.Enter(brain);
            agent.HealthPercent = BelowThreshold;
            gate.Tick(brain, 0.1f); // recovery starts

            gate.Tick(brain, GiveUpMinutes + 1f); // standing at home, health flat past the give-up budget
            Assert.AreEqual(2, inner.Entered, "no zone at home: the NPC gives up and resumes the routine");

            gate.Tick(brain, 0.1f);
            Assert.AreEqual(2, inner.Entered, "still below the threshold but gave up — no retry loop");

            agent.HealthPercent = AboveThreshold; // healed by other means: the gate re-arms
            gate.Tick(brain, 0.1f);
            agent.HealthPercent = BelowThreshold;
            gate.Tick(brain, 0.1f);
            Assert.AreEqual(2, inner.Exited, "re-armed gate retreats again");
        }

        [TestMethod]
        public void RecoveryInProgressSurvivesABattleInterruption()
        {
            var brain = CreateBrain(out var agent);
            var inner = new RecordingTask();
            var gate = CreateGate(inner);

            gate.Enter(brain);
            agent.HealthPercent = BelowThreshold;
            gate.Tick(brain, 0.1f); // recovering

            gate.Exit(brain);  // dragged into a battle
            gate.Enter(brain); // battle over

            Assert.AreEqual(1, inner.Entered, "the interrupted recovery resumes — the routine does not re-enter");

            agent.HealthPercent = 1f;
            gate.Tick(brain, 0.1f);
            Assert.AreEqual(2, inner.Entered);
        }

        // ---- helpers ----

        private static WorldBrain CreateBrain(out MutableAgent agent)
        {
            agent = new MutableAgent();
            return new WorldBrain(agent, new WorldBrainConfig { Aggressive = false }, new DefaultRandomNumberGenerator(seed: 1));
        }

        private static RecoveryGateActivity CreateGate(IWorldActivity inner) =>
            new(inner, new WorldActivityContext
            {
                Recovery = new RecoveryConfig { NpcRetreatHealthPercent = RetreatThreshold, NpcGiveUpMinutes = GiveUpMinutes },
            });

        private class RecordingTask : IWorldTask
        {
            public int Entered { get; private set; }
            public int Exited { get; private set; }
            public bool IsCompleted { get; private set; }

            public void Complete() => IsCompleted = true;
            public void Restart() => IsCompleted = false;
            public void Enter(WorldBrain brain) => Entered++;

            public void Tick(WorldBrain brain, float delta)
            {
            }

            public void Exit(WorldBrain brain) => Exited++;
        }

        private class NoopActivity : IWorldActivity
        {
            public void Enter(WorldBrain brain)
            {
            }

            public void Tick(WorldBrain brain, float delta)
            {
            }

            public void Exit(WorldBrain brain)
            {
            }
        }

        private class SettableClock : IWorldClock
        {
            public int Day => 0;
            public int Hour => MinuteOfDay / 60;
            public int Minute => MinuteOfDay % 60;
            public int MinuteOfDay { get; set; }
            public float NormalizedTimeOfDay => MinuteOfDay / 1440f;
            public DayPhase Phase => DayPhase.Day;

            public event Action<int>? HourPassed { add { } remove { } }
            public event Action<DayPhase>? PhaseChanged { add { } remove { } }

            public void Tick(float realDelta)
            {
            }

            public void RestoreState(int day, int minuteOfDay) => MinuteOfDay = minuteOfDay;
        }

        private class MutableAgent : IWorldAgent
        {
            public Vector2 Position => Vector2.Zero;
            public Vector2 HomePosition => Vector2.Zero; // always "at home" — recovery ticks in place
            public bool IsFighting => false;
            public float HealthPercent { get; set; } = 1f;

            public void MoveTo(Vector2 destination, float speed)
            {
            }

            public void StopMoving()
            {
            }

            public TargetSighting? GetSighting(float visionRadius) => null;

            public void SetActivityPose(string clip)
            {
            }

            public void ClearActivityPose()
            {
            }
        }
    }
}
