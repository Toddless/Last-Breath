namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Recovery;
    using Core.Ai.World.Time;
    using Core.Entity;
    using Core.Entity.Components;
    using Godot;
    using Moq;

    [TestClass]
    public class RestRecoveryTests
    {
        private const float MaxHealth = 1000f;
        private const float MaxMana = 500f;
        private const float MaxBarrier = 400f;

        [TestMethod]
        public void HealsAllThreeChannelsPerGameMinuteInsideAZone()
        {
            var (service, clock) = CreateService();
            var entity = CreateEntity(health: 500f, mana: 100f, barrier: 0f);
            service.RegisterZone(this, () => Vector2.Zero, radius: 100f);
            service.RegisterParticipant(entity.Object, () => new Vector2(50, 0));

            service.Tick(0.1f);         // baseline snapshot
            clock.MinuteOfDay = 2;      // two game minutes pass
            service.Tick(0.1f);

            // 10%/min health, 15%/min mana and barrier (config defaults) over 2 minutes.
            Assert.AreEqual(500f + MaxHealth * 0.1f * 2, entity.Object.CurrentHealth, 0.01f);
            Assert.AreEqual(100f + MaxMana * 0.15f * 2, entity.Object.CurrentMana, 0.01f);
            Assert.AreEqual(0f + MaxBarrier * 0.15f * 2, entity.Object.CurrentBarrier, 0.01f);
        }

        [TestMethod]
        public void DoesNotOvershootTheMaximum()
        {
            var (service, clock) = CreateService();
            var entity = CreateEntity(health: MaxHealth - 1f, mana: MaxMana, barrier: MaxBarrier);
            service.RegisterZone(this, () => Vector2.Zero, radius: 100f);
            service.RegisterParticipant(entity.Object, () => Vector2.Zero);

            service.Tick(0.1f);
            clock.MinuteOfDay = 60;
            service.Tick(0.1f);

            Assert.AreEqual(MaxHealth, entity.Object.CurrentHealth, 0.01f);
            Assert.AreEqual(MaxMana, entity.Object.CurrentMana, 0.01f);
            Assert.AreEqual(MaxBarrier, entity.Object.CurrentBarrier, 0.01f);
        }

        [TestMethod]
        public void FightingDeadAndOutOfZoneParticipantsAreNotHealed()
        {
            var (service, clock) = CreateService();
            var fighting = CreateEntity(health: 100f);
            fighting.SetupGet(e => e.IsFighting).Returns(true);
            var dead = CreateEntity(health: 100f);
            dead.SetupGet(e => e.IsAlive).Returns(false);
            var outside = CreateEntity(health: 100f);

            service.RegisterZone(this, () => Vector2.Zero, radius: 100f);
            service.RegisterParticipant(fighting.Object, () => Vector2.Zero);
            service.RegisterParticipant(dead.Object, () => Vector2.Zero);
            service.RegisterParticipant(outside.Object, () => new Vector2(500, 0));

            service.Tick(0.1f);
            clock.MinuteOfDay = 5;
            service.Tick(0.1f);

            Assert.AreEqual(100f, fighting.Object.CurrentHealth, 0.01f);
            Assert.AreEqual(100f, dead.Object.CurrentHealth, 0.01f);
            Assert.AreEqual(100f, outside.Object.CurrentHealth, 0.01f);
        }

        [TestMethod]
        public void ZoneEligibilityFilterBlocksUnwelcomeGuests()
        {
            var (service, clock) = CreateService();
            var welcome = CreateEntity(health: 100f);
            var unwelcome = CreateEntity(health: 100f);
            // Tracker #146: an NPC camp only rests those its owners don't consider an enemy.
            service.RegisterZone(this, () => Vector2.Zero, radius: 100f,
                canRest: guest => ReferenceEquals(guest, welcome.Object));
            service.RegisterParticipant(welcome.Object, () => Vector2.Zero);
            service.RegisterParticipant(unwelcome.Object, () => Vector2.Zero);

            service.Tick(0.1f);
            clock.MinuteOfDay = 2;
            service.Tick(0.1f);

            Assert.IsTrue(welcome.Object.CurrentHealth > 100f, "welcome guest heals");
            Assert.AreEqual(100f, unwelcome.Object.CurrentHealth, 0.01f, "unwelcome guest does not heal");
        }

        [TestMethod]
        public void UnregisteredZoneStopsHealing()
        {
            var (service, clock) = CreateService();
            var entity = CreateEntity(health: 100f);
            service.RegisterZone(this, () => Vector2.Zero, radius: 100f);
            service.RegisterParticipant(entity.Object, () => Vector2.Zero);
            service.UnregisterZone(this);

            service.Tick(0.1f);
            clock.MinuteOfDay = 5;
            service.Tick(0.1f);

            Assert.AreEqual(100f, entity.Object.CurrentHealth, 0.01f);
        }

        // ---- helpers ----

        private static (RestRecoveryService Service, SettableClock Clock) CreateService()
        {
            var clock = new SettableClock();
            var config = new Mock<IRecoveryConfigProvider>();
            config.SetupGet(c => c.Config).Returns(new RecoveryConfig());
            return (new RestRecoveryService(config.Object, clock, new LastBreathTest.WorldTesting.TestSpatialQuery()), clock);
        }

        private static Mock<IFightable> CreateEntity(float health, float mana = 0f, float barrier = 0f)
        {
            var parameters = new Mock<IEntityParametersComponent>();
            parameters.SetupGet(p => p.MaxHealth).Returns(MaxHealth);
            parameters.SetupGet(p => p.MaxMana).Returns(MaxMana);
            parameters.SetupGet(p => p.MaxBarrier).Returns(MaxBarrier);

            var entity = new Mock<IFightable>();
            entity.SetupGet(e => e.IsAlive).Returns(true);
            entity.SetupGet(e => e.IsFighting).Returns(false);
            entity.SetupGet(e => e.Parameters).Returns(parameters.Object);
            entity.SetupProperty(e => e.CurrentHealth, health);
            entity.SetupProperty(e => e.CurrentMana, mana);
            entity.SetupProperty(e => e.CurrentBarrier, barrier);
            return entity;
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
    }
}
