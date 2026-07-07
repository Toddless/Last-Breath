namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World;
    using Core.Ai.World.Time;
    using Core.Components;

    [TestClass]
    public class PlayerLifecycleTests
    {
        [TestMethod]
        public void RevivesAfterConfiguredGameHours()
        {
            var clock = new FakeClock();
            var lifecycle = NewLifecycle(clock, lieGameHours: 2f, burnChance: 0f);
            bool revived = false;
            lifecycle.ReviveReady += () => revived = true;

            lifecycle.OnDefeated();
            clock.AdvanceMinutes(119);
            lifecycle.Tick();
            Assert.AreEqual(PlayerLifeStage.Defeated, lifecycle.Stage);
            Assert.IsFalse(revived);

            clock.AdvanceMinutes(2);
            lifecycle.Tick();
            Assert.AreEqual(PlayerLifeStage.Alive, lifecycle.Stage);
            Assert.IsTrue(revived);
        }

        [TestMethod]
        public void TimerSurvivesMidnightRollover()
        {
            var clock = new FakeClock { Day = 1, MinuteOfDay = 23 * 60 }; // 23:00
            var lifecycle = NewLifecycle(clock, lieGameHours: 4f, burnChance: 0f);
            bool revived = false;
            lifecycle.ReviveReady += () => revived = true;

            lifecycle.OnDefeated();
            clock.Day = 2;
            clock.MinuteOfDay = 2 * 60; // 02:00 next day: 3 of 4 hours passed
            lifecycle.Tick();
            Assert.IsFalse(revived);

            clock.MinuteOfDay = 3 * 60 + 1;
            lifecycle.Tick();
            Assert.IsTrue(revived);
        }

        [TestMethod]
        public void EachPasserByRollsExactlyOnce()
        {
            var rnd = new SequencedRandom(0.9f, 0.9f, 0.1f); // fail, fail, success
            var lifecycle = NewLifecycle(new FakeClock(), lieGameHours: 4f, burnChance: 0.5f, rnd);
            lifecycle.OnDefeated();

            Assert.IsFalse(lifecycle.TryBurnRoll("npc_1")); // rolled 0.9 > 0.5
            Assert.IsFalse(lifecycle.TryBurnRoll("npc_1")); // same NPC: no re-roll (0.9 stays queued)
            Assert.IsFalse(lifecycle.TryBurnRoll("npc_2")); // rolled 0.9
            Assert.IsTrue(lifecycle.TryBurnRoll("npc_3"));  // rolled 0.1 <= 0.5
            Assert.AreEqual(PlayerLifeStage.FinalDead, lifecycle.Stage);
        }

        [TestMethod]
        public void BurnedCorpseNeverRevives()
        {
            var clock = new FakeClock();
            var lifecycle = NewLifecycle(clock, lieGameHours: 1f, burnChance: 1f);
            bool burned = false;
            bool revived = false;
            lifecycle.Burned += () => burned = true;
            lifecycle.ReviveReady += () => revived = true;

            lifecycle.OnDefeated();
            Assert.IsTrue(lifecycle.TryBurnRoll("npc_1"));
            Assert.IsTrue(burned);

            clock.AdvanceMinutes(10000);
            lifecycle.Tick();
            Assert.AreEqual(PlayerLifeStage.FinalDead, lifecycle.Stage);
            Assert.IsFalse(revived);
        }

        [TestMethod]
        public void AliveBodyCannotBeBurned()
        {
            var lifecycle = NewLifecycle(new FakeClock(), lieGameHours: 1f, burnChance: 1f);

            Assert.IsFalse(lifecycle.TryBurnRoll("npc_1"));
            Assert.AreEqual(PlayerLifeStage.Alive, lifecycle.Stage);
        }

        private static PlayerLifecycle NewLifecycle(FakeClock clock, float lieGameHours, float burnChance, IRandomNumberGenerator? rnd = null) =>
            new(new PlayerLifecycleConfig { LieGameHours = lieGameHours, BurnChance = burnChance }, clock, rnd ?? new SequencedRandom(0f));

        private sealed class FakeClock : IWorldClock
        {
            public int Day { get; set; } = 1;
            public int MinuteOfDay { get; set; }
            public int Hour => MinuteOfDay / 60;
            public int Minute => MinuteOfDay % 60;
            public float NormalizedTimeOfDay => MinuteOfDay / 1440f;
            public DayPhase Phase => DayPhase.Day;

            public event Action<int>? HourPassed { add { } remove { } }
            public event Action<DayPhase>? PhaseChanged { add { } remove { } }

            public void Tick(float realDelta)
            {
            }

            public void RestoreState(int day, int minuteOfDay)
            {
                Day = day;
                MinuteOfDay = minuteOfDay;
            }

            public void AdvanceMinutes(int minutes)
            {
                int total = MinuteOfDay + minutes;
                Day += total / 1440;
                MinuteOfDay = total % 1440;
            }
        }

        private sealed class SequencedRandom(params float[] values) : IRandomNumberGenerator
        {
            private int _index;

            public float RandFloat() => values[Math.Min(_index++, values.Length - 1)];
            public float RandFloatRange(float min, float max) => min;
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
