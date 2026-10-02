namespace LastBreathTest.Ai
{
    using Core.Ai.World;
    using Core.Ai.World.Raids;
    using Core.Data.GameData;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Reputation;
    using Core.Services;
    using Godot;
    using Moq;
    using Newtonsoft.Json;
    using Reputation;

    [TestClass]
    public class RaidServiceTests
    {
        private Mock<IPlayer> _player = null!;
        private TestSpatialQuery _spatial = null!;
        private Core.World.Spaces.BattleSiteRegistry _battleSites = null!;
        private GameEventBus _bus = null!;
        private FactionRelationService _relations = null!;
        private NpcPopulationService _population = null!;
        private RaidSpawnRegistry _sites = null!;
        private FakeSpawner _spawner = null!;
        private TestRaidService _raids = null!;
        private List<Mock<IFightableNpc>> _raiderMocks = null!;
        private List<RaidStartedEvent> _started = null!;
        private List<RaidEndedEvent> _ended = null!;
        private List<NpcFinalDeathEvent> _finalDeaths = null!;

        [TestInitialize]
        public void Setup()
        {
            var localization = new Mock<ILocalizationService>();
            localization.Setup(l => l.Localize(It.IsAny<string>())).Returns<string>(key => key);
            Localization.Override(localization.Object);

            _bus = new GameEventBus();
            _relations = new FactionRelationService(FactionTestData.Create());
            _population = new NpcPopulationService(_bus);
            _sites = new RaidSpawnRegistry();
            _spawner = new FakeSpawner();
            _raiderMocks = [];
            _started = [];
            _ended = [];
            _finalDeaths = [];
            _spawner.Factory = CreateRaiderMock;

            _bus.Subscribe<RaidStartedEvent>(_started.Add);
            _bus.Subscribe<RaidEndedEvent>(_ended.Add);
            _bus.Subscribe<NpcFinalDeathEvent>(_finalDeaths.Add);

            var player = _player = new Mock<IPlayer>();
            _spatial = new TestSpatialQuery();
            _battleSites = new Core.World.Spaces.BattleSiteRegistry();
            player.SetupGet(p => p.IsAlive).Returns(true);
            player.SetupGet(p => p.IsFighting).Returns(false);
            var accessor = new Mock<IPlayerAccessor>();
            accessor.SetupGet(a => a.Player).Returns(player.Object);

            var provider = new Mock<INpcProvider>();
            provider.Setup(p => p.CreateDefinition(It.IsAny<string>())).Returns<string>(Definition);

            var messages = new Mock<IGameMessageBus>();
            messages.Setup(m => m.PublishMessageAsync(It.IsAny<SendNotificationMessageMessage>())).Returns(Task.CompletedTask);

            _raids = new TestRaidService(_relations, _sites, provider.Object, _spawner, _population, accessor.Object, _bus, messages.Object, new FixedRaidRandom(), _battleSites, _spatial);
            _raids.Apply(DataCatalog.Raids, ConfigFile(initialDelay: 0));
        }

        [TestMethod]
        public void UnloadedRaidExpiresWithoutDereferencingItsFormerNodes()
        {
            StartRaid();
            _raids.SuspendSpace(new object());
            foreach (var raider in _raiderMocks) raider.SetupGet(x => x.IsAlive).Throws(new InvalidOperationException("Freed node"));
            Assert.AreEqual(0, _finalDeaths.Count);
            _raids.Tick(20);
            Assert.IsFalse(_raids.IsRaidActive);
            Assert.AreEqual(2, _finalDeaths.Count);
            Assert.AreEqual(0, _population.CurrentCount);
        }

        [TestMethod]
        public void ResumedRaidUsesRestoredResidents()
        {
            StartRaid();
            var originals = _raiderMocks.ToList();
            _raids.SuspendSpace(new object());
            var replacements = originals.Select(old =>
            {
                var next = new Mock<IFightableNpc>();
                next.SetupGet(x => x.InstanceId).Returns(old.Object.InstanceId);
                next.SetupGet(x => x.IsAlive).Returns(false);
                return next.Object;
            }).ToList();
            _raids.ResumeResidents(replacements);
            _raids.Tick(1);
            Assert.IsFalse(_raids.IsRaidActive);
            Assert.AreEqual(0, _finalDeaths.Count);
        }

        [TestMethod]
        public void RaidSpawnsFromTheNearestHatredSite()
        {
            _relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Hatred);
            _sites.Register(new FakeSite(Fractions.Elf, new Vector2(1000, 0), "elf_warrior"));
            _sites.Register(new FakeSite(Fractions.Elf, new Vector2(200, 0), "elf_warrior"));
            _sites.Register(new FakeSite(Fractions.Human, new Vector2(50, 0), "villager"));

            _raids.Tick(1f);

            Assert.IsTrue(_raids.IsRaidActive);
            Assert.AreEqual(2, _spawner.Spawned.Count); // squadSizeMin = squadSizeMax = 2
            Assert.AreEqual(new Vector2(200, 0), _started.Single().Origin); // the nearest elf home, not the human one
            Assert.AreEqual(2, _population.CurrentCount); // reserved outside the limit
        }

        [TestMethod]
        public void NoRaidBelowHatredOrFromNonRaidingFactions()
        {
            _relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Hostility);
            _relations.SetPlayerRelation(Fractions.Demon, RelationLevel.Hatred); // canRaid = false
            _sites.Register(new FakeSite(Fractions.Elf, Vector2.Zero, "elf_warrior"));
            _sites.Register(new FakeSite(Fractions.Demon, Vector2.Zero, "imp"));

            _raids.Tick(1f);

            Assert.IsFalse(_raids.IsRaidActive);
            Assert.AreEqual(0, _spawner.Spawned.Count);
        }

        [TestMethod]
        public void CooldownBlocksRaidsAndTicksDown()
        {
            _raids.Apply(DataCatalog.Raids, ConfigFile(initialDelay: 100));
            _relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Hatred);
            _sites.Register(new FakeSite(Fractions.Elf, Vector2.Zero, "elf_warrior"));

            _raids.Tick(1f);

            Assert.IsFalse(_raids.IsRaidActive);
            Assert.AreEqual(99f, _raids.CooldownRemaining, 0.001f);
        }

        [TestMethod]
        public void RaidersAreDrivenStraightAtThePlayer()
        {
            StartRaid();

            _raids.Tick(1f);

            foreach (var raider in _raiderMocks)
                raider.As<IWorldAgent>().Verify(a => a.MoveTo(_raids.PlayerPoint, It.IsAny<float>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void TimeoutDespawnsSurvivorsThroughTheFinalDeathChannel()
        {
            StartRaid(); // duration = 10

            _raids.Tick(11f);

            Assert.IsFalse(_raids.IsRaidActive);
            Assert.AreEqual(2, _spawner.Despawned.Count);
            Assert.AreEqual(2, _finalDeaths.Count);
            Assert.AreEqual(0, _population.CurrentCount); // the final-death events released the slots
            Assert.AreEqual(1, _ended.Count);
            Assert.AreEqual(900f, _raids.CooldownRemaining, 0.001f);
        }

        [TestMethod]
        public void WipedRaidEndsWithoutDespawnAndKeepsTheBodiesReserved()
        {
            StartRaid();
            foreach (var raider in _raiderMocks)
                raider.SetupGet(n => n.IsAlive).Returns(false);

            _raids.Tick(1f);

            Assert.IsFalse(_raids.IsRaidActive);
            Assert.AreEqual(0, _spawner.Despawned.Count); // bodies stay in the world
            Assert.AreEqual(2, _population.CurrentCount); // slots free when the bodies burn
            Assert.AreEqual(1, _ended.Count);
        }

        [TestMethod]
        public void FightingRaiderDelaysTheWrapUp()
        {
            StartRaid();
            _raiderMocks[0].SetupGet(n => n.IsFighting).Returns(true);

            _raids.Tick(11f); // timeout passed, but the battle plays out first

            Assert.IsTrue(_raids.IsRaidActive);

            _raiderMocks[0].SetupGet(n => n.IsFighting).Returns(false);
            _raids.Tick(0.1f);
            Assert.IsFalse(_raids.IsRaidActive);
        }

        private void StartRaid()
        {
            _relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Hatred);
            _sites.Register(new FakeSite(Fractions.Elf, new Vector2(200, 0), "elf_warrior"));
            _raids.Tick(1f);
            Assert.IsTrue(_raids.IsRaidActive);
        }

        [TestMethod]
        public void FightingPlayerIsReplacedByTheOriginMarker()
        {
            StartRaid();
            _player.SetupGet(p => p.IsFighting).Returns(true);
            _spatial.Set(_player.Object, 2);
            _raids.PlayerPoint = new Vector2(9000, 9000);
            var site = new Core.World.Spaces.BattleSite(Guid.NewGuid(), 1, new Vector2(120, 80));
            _battleSites.Register(site);
            _raids.Tick(1);
            foreach (var raider in _raiderMocks)
            {
                raider.As<IWorldAgent>().Verify(a => a.MoveTo(site.Position, It.IsAny<float>()), Times.Once);
                raider.As<IWorldAgent>().Verify(a => a.MoveTo(_raids.PlayerPoint, It.IsAny<float>()), Times.Never);
            }
            _battleSites.Remove(site.BattleId);
            _raids.Tick(1);
            foreach (var raider in _raiderMocks)
                raider.As<IWorldAgent>().Verify(a => a.StopMoving(), Times.Once);
        }

        [TestMethod]
        public void LeavingTheRaidSpaceStopsPursuitButPreservesExpiry()
        {
            StartRaid();
            _spatial.Set(_player.Object, 2);
            _raids.Tick(1);
            foreach (var raider in _raiderMocks)
            {
                raider.As<IWorldAgent>().Verify(a => a.StopMoving(), Times.Once);
                raider.As<IWorldAgent>().Verify(a => a.MoveTo(It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
            }
            Assert.IsTrue(_raids.IsRaidActive);
            _raids.Tick(11);
            Assert.IsFalse(_raids.IsRaidActive);
            Assert.AreEqual(2, _spawner.Despawned.Count);
        }

        [TestMethod]
        public void MarkerInAnotherSpaceCannotAttractRaiders()
        {
            StartRaid();
            _player.SetupGet(p => p.IsFighting).Returns(true);
            _battleSites.Register(new Core.World.Spaces.BattleSite(Guid.NewGuid(), 2, Vector2.Zero));
            _raids.Tick(1);
            foreach (var raider in _raiderMocks)
                raider.As<IWorldAgent>().Verify(a => a.MoveTo(It.IsAny<Vector2>(), It.IsAny<float>()), Times.Never);
        }

        [TestMethod]
        public void ClosestSpawnSiteMustBelongToPlayerSpace()
        {
            _relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Hatred);
            var foreign = new FakeSite(Fractions.Elf, Vector2.Zero, "elf_warrior");
            _spatial.Set(foreign, 2);
            _sites.Register(foreign);
            _sites.Register(new FakeSite(Fractions.Elf, new Vector2(500, 0), "elf_warrior"));
            Assert.IsTrue(_raids.ForceRaid());
            Assert.AreEqual(new Vector2(500, 0), _started.Single().Origin);
        }

        private IFightableNpc CreateRaiderMock()
        {
            var npc = new Mock<IFightableNpc>();
            npc.SetupGet(n => n.IsAlive).Returns(true);
            npc.SetupGet(n => n.IsFighting).Returns(false);
            npc.SetupGet(n => n.InstanceId).Returns($"raider-{_raiderMocks.Count}");
            npc.SetupGet(n => n.Position).Returns(Vector2.Zero);
            npc.As<IWorldAgent>();
            _raiderMocks.Add(npc);
            return npc.Object;
        }

        private static NpcDefinition Definition(string npcId) => new()
        {
            NpcId = npcId,
            Fraction = Fractions.Elf,
            Parameters = new Dictionary<EntityParameter, float>(),
            Abilities = [],
            Lifecycle = new NpcLifecycleConfig(),
        };

        private static GameDataFile ConfigFile(float initialDelay) => new("Raids.json", JsonConvert.SerializeObject(new RaidsData
        {
            InitialDelaySeconds = initialDelay,
            CheckIntervalSeconds = 5,
            Chance = 1f,
            CooldownSeconds = 900,
            DurationSeconds = 10,
            SquadSizeMin = 2,
            SquadSizeMax = 2,
            MoveSpeed = 220,
            SpawnJitter = 0,
        }));

        /// <summary>Exposes a deterministic player position — the real one needs a Godot node in a tree.</summary>
        private sealed class TestRaidService(
            IFactionRelationService relations, IRaidSpawnRegistry sites, INpcProvider provider, INpcWorldSpawner spawner,
            INpcPopulationService population, IPlayerAccessor accessor, GameEventBus bus, IGameMessageBus messages, IRandomNumberGenerator rnd, Core.World.Spaces.BattleSiteRegistry battleSites, TestSpatialQuery spatial)
            : RaidService(relations, sites, provider, spawner, population, accessor, bus, messages, rnd, battleSites, spatial)
        {
            public Vector2 PlayerPoint { get; set; } = new(0, 0);

            protected override Vector2? GetPlayerPosition() => PlayerPoint;
        }

        private sealed class FakeSpawner : INpcWorldSpawner
        {
            public List<IFightableNpc> Spawned { get; } = [];
            public List<IFightableNpc> Despawned { get; } = [];
            public Func<IFightableNpc>? Factory { get; set; }

            public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position)
            {
                var npc = Factory!();
                Spawned.Add(npc);
                return npc;
            }

            public void Despawn(IFightableNpc npc) => Despawned.Add(npc);
        }

        private sealed class FakeSite(Fractions fraction, Vector2 position, params string[] ids) : IRaidSpawnSite
        {
            public Fractions? Fraction => fraction;
            public Vector2 Position => position;
            public IReadOnlyList<string> NpcIds => ids;
        }

        private sealed class FixedRaidRandom : IRandomNumberGenerator
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
