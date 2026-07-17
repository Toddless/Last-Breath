namespace LastBreathTest.WorldTesting
{
    using Core.Ai.World.Time;

    [TestClass]
    public class WorldClockTests
    {
        [TestMethod]
        public void StartsAtConfiguredTime()
        {
            var clock = CreateClock(startHour: 8);

            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(8, clock.Hour);
            Assert.AreEqual(0, clock.Minute);
            Assert.AreEqual(DayPhase.Morning, clock.Phase); // 8:00 is inside Morning (06–10)
        }

        [TestMethod]
        public void RealSecondsScaleIntoGameHours()
        {
            // 24 real minutes per day => 60 real seconds per game hour.
            var clock = CreateClock(startHour: 8);

            clock.Tick(60f);

            Assert.AreEqual(9, clock.Hour);
        }

        [TestMethod]
        public void MidnightWrapsToTheNextDay()
        {
            var clock = CreateClock(startHour: 23);

            clock.Tick(2 * 60f); // two game hours: 23:00 -> 01:00 next day

            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(1, clock.Hour);
        }

        [TestMethod]
        public void HourAndPhaseEventsFire()
        {
            var clock = CreateClock(startHour: 9);
            int? hourReported = null;
            DayPhase? phaseReported = null;
            clock.HourPassed += hour => hourReported = hour;
            clock.PhaseChanged += phase => phaseReported = phase;

            clock.Tick(60f); // 9:00 -> 10:00 crosses the Morning/Day boundary

            Assert.AreEqual(10, hourReported);
            Assert.AreEqual(DayPhase.Day, phaseReported);
        }

        [TestMethod]
        public void PhasesMatchTheAgreedBoundaries()
        {
            var clock = CreateClock(startHour: 0);
            Assert.AreEqual(DayPhase.Night, clock.Phase);

            Assert.AreEqual(DayPhase.Night, PhaseAtHour(5));
            Assert.AreEqual(DayPhase.Morning, PhaseAtHour(6));
            Assert.AreEqual(DayPhase.Day, PhaseAtHour(10));
            Assert.AreEqual(DayPhase.Evening, PhaseAtHour(18));
            Assert.AreEqual(DayPhase.Night, PhaseAtHour(22));
        }

        private static DayPhase PhaseAtHour(int hour) => CreateClock(startHour: hour).Phase;

        private static WorldClock CreateClock(int startHour) => new(new WorldClockConfig
        {
            RealMinutesPerGameDay = 24f,
            StartDay = 1,
            StartHour = startHour,
        });
    }
}
