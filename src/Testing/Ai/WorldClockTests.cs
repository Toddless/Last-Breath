namespace LastBreathTest.Ai
{
    using Core.Ai.World.Time;
    using Core.Session;

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

        /// <summary>The rewind that opens a restore crosses phases without a word — only the file's own
        /// time, applied after it, announces what differs from the rewound start. So the pair of events
        /// carries no promise that a phase change was reported at all.</summary>
        [TestMethod]
        public void SessionResetRewindsTheClockWithoutAnnouncingIt()
        {
            var clock = CreateClock(startHour: 8); // Morning
            clock.Tick(14 * 60f); // 08:00 -> 22:00, Night
            int announcements = 0;
            clock.HourPassed += _ => announcements++;
            clock.PhaseChanged += _ => announcements++;
            clock.MinutePassed += _ => announcements++;

            ((ISessionResettable)clock).ResetSession();

            Assert.AreEqual(DayPhase.Morning, clock.Phase);
            Assert.AreEqual(0, announcements, "the session rewind is silent by design");
        }

        [TestMethod]
        public void ATickCrossingAMinuteAnnouncesItOnceWithTheCurrentTime()
        {
            var clock = CreateClock(startHour: 8);
            clock.Tick(0.5f); // 24 real min/day => one real second per game minute: 08:00:30
            var announced = RecordMinutes(clock);

            clock.Tick(0.75f); // 08:00:30 -> 08:01:15

            CollectionAssert.AreEqual(new List<double> { clock.TotalMinutes }, announced);
        }

        [TestMethod]
        public void ATickWithinTheSameMinuteStaysSilent()
        {
            var clock = CreateClock(startHour: 8);
            var announced = RecordMinutes(clock);

            clock.Tick(0.25f); // 08:00:15
            clock.Tick(0.5f); // 08:00:45

            Assert.AreEqual(0, announced.Count);
        }

        [TestMethod]
        public void ATickSpanningSeveralMinutesAnnouncesOnlyTheFinalTime()
        {
            var clock = CreateClock(startHour: 8);
            var announced = RecordMinutes(clock);

            clock.Tick(5.5f); // 08:00 -> 08:05:30 in one tick

            CollectionAssert.AreEqual(new List<double> { clock.TotalMinutes }, announced);
        }

        [TestMethod]
        public void ARestoreJumpAnnouncesTheNewTime()
        {
            var clock = CreateClock(startHour: 8);
            var announced = RecordMinutes(clock);

            clock.RestoreTime(clock.TotalMinutes + 90.5); // 08:00 -> 09:30:30

            CollectionAssert.AreEqual(new List<double> { clock.TotalMinutes }, announced);
        }

        [TestMethod]
        public void RestoringWithinTheSameMinuteStaysSilent()
        {
            var clock = CreateClock(startHour: 8);
            clock.Tick(0.25f); // 08:00:15
            var announced = RecordMinutes(clock);

            clock.RestoreTime(clock.TotalMinutes + 0.5); // 08:00:15 -> 08:00:45

            Assert.AreEqual(0, announced.Count);
        }

        /// <summary>Collects every time the clock announces a passed minute from now on.</summary>
        private static List<double> RecordMinutes(WorldClock clock)
        {
            var announced = new List<double>();
            clock.MinutePassed += announced.Add;
            return announced;
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
