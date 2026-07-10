namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Skirmish;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;
    using Core.Reputation;
    using Core.Services;
    using Godot;
    using Moq;

    [TestClass]
    public class WorldWitnessQueryTests
    {
        private const float Radius = 600f;

        private GameEventBus _bus = null!;
        private NpcWorldRegistry _registry = null!;
        private WorldWitnessQuery _query = null!;

        [TestInitialize]
        public void Setup()
        {
            _bus = new GameEventBus();
            _registry = new NpcWorldRegistry();
            _query = new WorldWitnessQuery(_registry, new FactionRelationService(FactionTestData.Create()), _bus);
        }

        [TestMethod]
        public void BystanderInRadiusWitnesses()
        {
            _registry.Register(Bystander(Fractions.Human, new Vector2(300, 0)));

            Assert.IsTrue(_query.HasWitness(Vector2.Zero, Radius));
        }

        [TestMethod]
        public void FarAwayFightingAndDeadNpcsDoNotWitness()
        {
            _registry.Register(Bystander(Fractions.Human, new Vector2(2000, 0)));
            _registry.Register(Bystander(Fractions.Human, new Vector2(100, 0), isFighting: true));
            _registry.Register(Bystander(Fractions.Human, new Vector2(100, 0), isAlive: false));

            Assert.IsFalse(_query.HasWitness(Vector2.Zero, Radius));
        }

        [TestMethod]
        public void BeastsCannotSpreadTheWord()
        {
            _registry.Register(Bystander(Fractions.Animal, new Vector2(100, 0)));

            Assert.IsFalse(_query.HasWitness(Vector2.Zero, Radius));
        }

        [TestMethod]
        public void TheVictimItselfIsExcluded()
        {
            _registry.Register(Bystander(Fractions.Elf, new Vector2(100, 0), instanceId: "victim"));

            Assert.IsFalse(_query.HasWitness(Vector2.Zero, Radius, excludeInstanceId: "victim"));
        }

        [TestMethod]
        public void ActiveBattleAnchorsTheLookupAtItsWorldSite()
        {
            var siteNpc = FightableNpc(Fractions.Elf, new Vector2(5000, 5000));
            _bus.Publish(new BattleInitializedEvent(new Mock<IPlayer>().Object, [siteNpc]));
            _registry.Register(Bystander(Fractions.Human, new Vector2(5100, 5000))); // near the site, far from the arena

            // The deed reports an arena-spot position; the site captured at battle start wins.
            Assert.IsTrue(_query.HasWitness(Vector2.Zero, Radius));
        }

        [TestMethod]
        public void FledNpcIsAGuaranteedWitnessUntilTheBattleEnds()
        {
            _bus.Publish(new EntityFledBattleEvent(FightableNpc(Fractions.Elf, Vector2.Zero)));

            Assert.IsTrue(_query.HasWitness(new Vector2(99999, 99999), Radius));

            _bus.Publish(new BattleEndEvent(BattleResults.PlayerWon));
            Assert.IsFalse(_query.HasWitness(new Vector2(99999, 99999), Radius));
        }

        [TestMethod]
        public void FledBeastIsNotAWitness()
        {
            _bus.Publish(new EntityFledBattleEvent(FightableNpc(Fractions.Animal, Vector2.Zero)));

            Assert.IsFalse(_query.HasWitness(new Vector2(99999, 99999), Radius));
        }

        private static ISkirmishParticipant Bystander(Fractions fraction, Vector2 position, bool isAlive = true, bool isFighting = false, string instanceId = "bystander")
        {
            var npc = new Mock<ISkirmishParticipant>();
            npc.SetupGet(n => n.Fraction).Returns(fraction);
            npc.SetupGet(n => n.Position).Returns(position);
            npc.SetupGet(n => n.IsAlive).Returns(isAlive);
            npc.SetupGet(n => n.IsFighting).Returns(isFighting);
            npc.SetupGet(n => n.InstanceId).Returns(instanceId);
            return npc.Object;
        }

        private static IFightableNpc FightableNpc(Fractions fraction, Vector2 position)
        {
            var npc = new Mock<IFightableNpc>();
            npc.SetupGet(n => n.Fraction).Returns(fraction);
            npc.SetupGet(n => n.Position).Returns(position);
            npc.SetupGet(n => n.InstanceId).Returns("fled-npc");
            return npc.Object;
        }
    }
}
