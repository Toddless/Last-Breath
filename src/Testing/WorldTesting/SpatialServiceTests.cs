namespace LastBreathTest.WorldTesting
{
    using Core.World.Spaces;
    using Core.Ai.World;
    using Core.Ai.World.Activities;
    using Core.Ai.World.SmartPoints;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Recovery;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Services;
    using Core.Reputation;
    using Godot;
    using Moq;

    [TestClass]
    public class SpatialServiceTests
    {
        [TestMethod]
        public void UnknownNativeObjectsNeverShareASpace()
        {
            Assert.IsFalse(NativeSpatialQuery.Instance.SharesSpace(new object(), new object()));
            Assert.AreEqual(0UL, NativeSpatialQuery.Instance.GetSpace(null));
        }

        [TestMethod]
        public void SmartPointClaimsAreScopedAndOldClaimsAreReleased()
        {
            var spatial = new TestSpatialQuery();
            var registry = new SmartPointRegistry(spatial);
            var owner = new object();
            var local = Point(new Vector2(50, 0));
            var foreign = Point(Vector2.Zero);
            spatial.Set(foreign, 2);
            registry.Register(foreign);
            registry.Register(local);
            Assert.AreSame(local, registry.TryClaim("test", "owner", Vector2.Zero, owner));
            spatial.Set(owner, 2);
            Assert.AreSame(foreign, registry.TryClaim("test", "owner", Vector2.Zero, owner));
            Assert.AreSame(local, registry.TryClaim("test", "other", Vector2.Zero, new object()));
            spatial.Set(owner, 0);
            Assert.IsNull(registry.TryClaim("test", "owner", Vector2.Zero, owner));
        }

        [TestMethod]
        public void RecoveryZonesDoNotHealAnotherSpace()
        {
            const float maxHealth = 100f;
            const float wounded = 50f;
            const float healthRate = 0.1f;
            var spatial = new TestSpatialQuery();
            var config = new Mock<IRecoveryConfigProvider>();
            config.SetupGet(c => c.Config).Returns(new RecoveryConfig { HealthPercentPerMinute = healthRate });
            var recovery = new RestRecoveryService(config.Object, spatial: spatial);
            var parameters = new Mock<IEntityParametersComponent>();
            parameters.SetupGet(p => p.MaxHealth).Returns(maxHealth);
            var fighter = new Mock<IFightable>();
            fighter.SetupGet(p => p.IsAlive).Returns(true);
            fighter.SetupGet(p => p.Parameters).Returns(parameters.Object);
            fighter.SetupProperty(p => p.CurrentHealth, wounded);
            var zone = new object();
            spatial.Set(zone, 2);
            recovery.RegisterZone(zone, () => Vector2.Zero, 100);
            recovery.RegisterParticipant(fighter.Object, () => Vector2.Zero);
            recovery.Tick(1);
            recovery.Tick(1);
            Assert.AreEqual(wounded, fighter.Object.CurrentHealth);
            spatial.Set(zone, 1);
            recovery.Tick(1); // no clock: one real second stands for one game minute
            Assert.AreEqual(wounded + maxHealth * healthRate, fighter.Object.CurrentHealth, 0.01f);
        }

        [TestMethod]
        public void HuntingSkipsCloserPreyFromAnotherSpace()
        {
            var spatial = new TestSpatialQuery();
            var self = Prey(Fractions.Human, Vector2.Zero);
            var local = Prey(Fractions.Animal, new Vector2(50, 0));
            var foreign = Prey(Fractions.Animal, new Vector2(1, 0));
            spatial.Set(foreign, 2);
            var npcs = new NpcWorldRegistry();
            npcs.Register(foreign);
            npcs.Register(local);
            var relations = new Mock<IFactionRelationService>();
            relations.Setup(r => r.IsHostile(Fractions.Human, Fractions.Animal)).Returns(true);
            var context = new WorldActivityContext { Self = self, Npcs = npcs, Relations = relations.Object, Spatial = spatial };
            var agent = new Mock<IWorldAgent>();
            var brain = new WorldBrain(agent.Object, new WorldBrainConfig { LeashRadius = 1000 }, new DefaultRandomNumberGenerator(), context);
            new HuntActivity(context).Tick(brain, 1);
            agent.Verify(a => a.MoveTo(local.Position, It.IsAny<float>()), Times.Once);
            agent.Verify(a => a.MoveTo(foreign.Position, It.IsAny<float>()), Times.Never);
        }

        [TestMethod]
        public void WitnessesNeedTheDeedsSpace()
        {
            var spatial = new TestSpatialQuery();
            var npcs = new NpcWorldRegistry();
            var witness = Prey(Fractions.Human, Vector2.Zero);
            npcs.Register(witness);
            spatial.Set(witness, 2);
            var relations = new Mock<IFactionRelationService>();
            relations.Setup(r => r.HasReputation(Fractions.Human)).Returns(true);
            var query = new WorldWitnessQuery(npcs, relations.Object, new GameEventBus(), spatial: spatial);
            Assert.IsFalse(query.HasWitness(Vector2.Zero, 100));
            spatial.Set(witness, 1);
            Assert.IsTrue(query.HasWitness(Vector2.Zero, 100));
        }

        [TestMethod]
        public void StaleMarkerRemovalCannotRemoveTheNextBattle()
        {
            var registry = new BattleSiteRegistry();
            var first = new BattleSite(Guid.NewGuid(), 1, Vector2.Zero);
            var second = new BattleSite(Guid.NewGuid(), 2, Vector2.One);
            registry.Register(first);
            Assert.ThrowsException<InvalidOperationException>(() => registry.Register(second));
            registry.Remove(first.BattleId);
            registry.Register(second);
            registry.Remove(first.BattleId);
            Assert.AreEqual(second, registry.Current);
            registry.ResetSession();
            Assert.IsNull(registry.Current);
        }

        private static ISmartPoint Point(Vector2 position)
        {
            var point = new Mock<ISmartPoint>();
            point.SetupGet(p => p.Tag).Returns("test");
            point.SetupGet(p => p.Capacity).Returns(1);
            point.SetupGet(p => p.Position).Returns(position);
            return point.Object;
        }

        private static ISkirmishParticipant Prey(Fractions faction, Vector2 position)
        {
            var npc = new Mock<ISkirmishParticipant>();
            npc.SetupGet(n => n.IsAlive).Returns(true);
            npc.SetupGet(n => n.Fraction).Returns(faction);
            npc.SetupGet(n => n.Position).Returns(position);
            npc.SetupGet(n => n.InstanceId).Returns(Guid.NewGuid().ToString());
            return npc.Object;
        }
    }
}
