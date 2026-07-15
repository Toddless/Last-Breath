namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World;
    using Core.Ai.World.Activities;
    using Core.Ai.World.Time;
    using Core.Entity.Components;
    using Godot;

    [TestClass]
    public class ScheduledActivityTests
    {
        [TestMethod]
        public void RunsTheSlotMatchingTheClock()
        {
            var clock = new FakeClock { MinuteOfDay = 12 * 60 };
            var day = new RecordingActivity();
            var night = new RecordingActivity();
            var scheduled = CreateScheduled(clock, day, night, out var brain);

            scheduled.Enter(brain);
            scheduled.Tick(brain, 0.1f);

            Assert.AreEqual(1, day.Entered);
            Assert.IsTrue(day.Ticks > 0);
            Assert.AreEqual(0, night.Entered);
        }

        [TestMethod]
        public void MidnightWrappingSlotMatchesBothSides()
        {
            var clock = new FakeClock { MinuteOfDay = 23 * 60 };
            var day = new RecordingActivity();
            var night = new RecordingActivity();
            var scheduled = CreateScheduled(clock, day, night, out var brain);

            scheduled.Enter(brain); // 23:00 — inside the 22:00–06:00 slot
            Assert.AreEqual(1, night.Entered);

            clock.MinuteOfDay = 3 * 60; // 03:00 — same wrapped slot, no re-enter
            scheduled.Tick(brain, 0.1f);
            Assert.AreEqual(1, night.Entered);
        }

        [TestMethod]
        public void SlotChangeExitsOldAndEntersNew()
        {
            var clock = new FakeClock { MinuteOfDay = 12 * 60 };
            var day = new RecordingActivity();
            var night = new RecordingActivity();
            var scheduled = CreateScheduled(clock, day, night, out var brain);
            scheduled.Enter(brain);

            clock.MinuteOfDay = 23 * 60; // the day slot ended
            scheduled.Tick(brain, 0.1f);

            Assert.AreEqual(1, day.Exited);
            Assert.AreEqual(1, night.Entered);
        }

        [TestMethod]
        public void GapsFallBackToTheBaseActivity()
        {
            var clock = new FakeClock { MinuteOfDay = 7 * 60 }; // 07:00 — no slot covers it
            var slotActivity = new RecordingActivity();
            var fallback = new RecordingActivity();
            var brain = CreateBrain();
            var scheduled = new ScheduledActivity(clock,
                [new ScheduleSlot(10 * 60, 22 * 60, slotActivity)], fallback);

            scheduled.Enter(brain);

            Assert.AreEqual(1, fallback.Entered);
            Assert.AreEqual(0, slotActivity.Entered);
        }

        // ---- helpers ----

        private static ScheduledActivity CreateScheduled(FakeClock clock, RecordingActivity day, RecordingActivity night, out WorldBrain brain)
        {
            brain = CreateBrain();
            return new ScheduledActivity(clock,
            [
                new ScheduleSlot(6 * 60, 22 * 60, day),
                new ScheduleSlot(22 * 60, 6 * 60, night),
            ], new RecordingActivity());
        }

        private static WorldBrain CreateBrain()
        {
            var agent = new StubAgent();
            return new WorldBrain(agent, new WorldBrainConfig { Aggressive = false }, new DefaultRandomNumberGenerator(seed: 1));
        }

        private class RecordingActivity : IWorldActivity
        {
            public int Entered { get; private set; }
            public int Exited { get; private set; }
            public int Ticks { get; private set; }

            public void Enter(WorldBrain brain) => Entered++;
            public void Tick(WorldBrain brain, float delta) => Ticks++;
            public void Exit(WorldBrain brain) => Exited++;
        }

        private class FakeClock : IWorldClock
        {
            public int Day => 1;
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

        private class StubAgent : IWorldAgent
        {
            public Vector2 Position => Vector2.Zero;
            public Vector2 HomePosition => Vector2.Zero;
            public bool IsFighting => false;

            public void MoveTo(Vector2 destination, float speed)
            {
            }

            public void StopMoving()
            {
            }

            public TargetSighting? GetSighting(float visionRadius) => null;
        }
    }
}
