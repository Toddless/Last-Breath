namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World;
    using Core.Ai.World.Time;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Reputation;
    using Core.Save.Participants;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class WorldSaveParticipantsTests
    {
        [TestMethod]
        public void WorldClockRoundTripsDayAndTime()
        {
            var source = new WorldClock(new WorldClockConfig { StartHour = 8 });
            source.Tick(60f * 30); // 24 real min/day => +30 game hours: day 2, 14:00
            var captured = new WorldClockSaveParticipant(source).Capture();

            var target = new WorldClock(new WorldClockConfig { StartHour = 8 });
            new WorldClockSaveParticipant(target).Restore(captured, 1);

            Assert.AreEqual(source.Day, target.Day);
            Assert.AreEqual(source.Hour, target.Hour);
            Assert.AreEqual(source.Minute, target.Minute);
            Assert.AreEqual(source.Phase, target.Phase);
        }

        [TestMethod]
        public void WorldClockRestoreFiresPhaseChangeForConsumers()
        {
            var clock = new WorldClock(new WorldClockConfig { StartHour = 12 }); // Day phase
            DayPhase? seen = null;
            clock.PhaseChanged += phase => seen = phase;

            clock.RestoreState(day: 3, minuteOfDay: 23 * 60); // 23:00 = Night

            Assert.AreEqual(DayPhase.Night, seen); // tint/schedules react without an extra tick
        }

        [TestMethod]
        public void FactionRelationsRoundTripPointsAndHysteresisState()
        {
            var source = new FactionRelationService(FactionTestData.Create());
            source.AddReputation(Fractions.Elf, 1100, "Deed"); // Friendly
            source.AddReputation(Fractions.Elf, -120, "Deed"); // 980 — Friendly held only by hysteresis
            source.SetPlayerRelation(Fractions.Dwarf, RelationLevel.Dislike);
            var captured = new FactionRelationsSaveParticipant(source).Capture();

            var target = new FactionRelationService(FactionTestData.Create());
            new FactionRelationsSaveParticipant(target).Restore(captured, 2);

            Assert.AreEqual(980, target.GetReputation(Fractions.Elf));
            Assert.AreEqual(RelationLevel.Friendly, target.GetPlayerRelation(Fractions.Elf)); // stateless resolve would say Neutral
            Assert.AreEqual(RelationLevel.Dislike, target.GetPlayerRelation(Fractions.Dwarf));
            Assert.AreEqual(-1000, target.GetReputation(Fractions.Undead)); // untouched default survives
        }

        [TestMethod]
        public void FactionRelationsMigrateV1LevelsToBandMidpoints()
        {
            var target = new FactionRelationService(FactionTestData.Create());
            var legacy = JToken.Parse("""{"playerRelations":{"Undead":"Hatred","Elf":"Dislike","Ghost":"Hatred"}}""");

            new FactionRelationsSaveParticipant(target).Restore(legacy, 1);

            Assert.AreEqual(RelationLevel.Hatred, target.GetPlayerRelation(Fractions.Undead));
            Assert.AreEqual((-6000 + -2000) / 2, target.GetReputation(Fractions.Undead));
            Assert.AreEqual(RelationLevel.Dislike, target.GetPlayerRelation(Fractions.Elf));
            Assert.AreEqual((-800 + -200) / 2, target.GetReputation(Fractions.Elf));
        }

        [TestMethod]
        public void LifecycleRestoreKeepsRemainingRiseTime()
        {
            var lifecycle = new NpcLifecycle(new NpcLifecycleConfig
            {
                ResurrectMinSeconds = 60f,
                ResurrectMaxSeconds = 600f,
                MaxStrengthBonus = 1f
            }, new FixedRandom());

            float? risingBonus = null;
            lifecycle.ResurrectionReady += bonus => risingBonus = bonus;

            lifecycle.RestoreState(NpcLifeStage.Defeated, resurrectDelay: 330f, elapsed: 300f);

            lifecycle.Tick(29f);
            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage); // 29 of the remaining 30 seconds
            lifecycle.Tick(2f);
            Assert.IsNotNull(risingBonus);
            Assert.AreEqual(0.5f, risingBonus.Value, 0.001f); // 330 is halfway between 60 and 600
        }

        [TestMethod]
        public void LifecycleRestoreDormantWaitsForBurning()
        {
            var lifecycle = new NpcLifecycle(new NpcLifecycleConfig(), new FixedRandom());

            lifecycle.RestoreState(NpcLifeStage.Dormant, 0f, 0f);
            lifecycle.Tick(100000f);

            Assert.AreEqual(NpcLifeStage.Dormant, lifecycle.Stage);
            Assert.IsTrue(lifecycle.CanBeBurned);
            Assert.IsTrue(lifecycle.TryBurn());
        }

        private sealed class FixedRandom : IRandomNumberGenerator
        {
            public float RandFloat() => 0.5f;
            public float RandFloatRange(float min, float max) => (min + max) / 2f;
            public int RandIntRange(int min, int max) => (min + max) / 2;
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
