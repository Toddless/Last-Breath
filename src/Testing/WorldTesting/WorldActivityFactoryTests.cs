namespace LastBreathTest.WorldTesting
{
    using Core.Ai.World;
    using Core.Ai.World.Activities;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.SmartPoints;
    using Core.Entity.Components;
    using Godot;
    using Moq;

    /// <summary>
    /// The Work activity: the same point-and-pose behavior Rest/Sleep are built from, with the
    /// point tag arriving as data (the <c>point</c> field of a schedule slot / routine step), so a
    /// smith and a trader are two data rows and not two classes.
    /// </summary>
    [TestClass]
    public class WorldActivityFactoryTests
    {
        [TestMethod]
        public void WorkTakesThePointTagFromData()
        {
            var stall = new FakePoint(SmartPointTags.TradeStall, new Vector2(200, 0));
            var registry = new FakeRegistry(stall);
            var agent = new FakeAgent();
            var brain = CreateBrain(agent, registry);

            // What a trader's schedule slot carries: {"activity": "Work", "point": "TradeStall"}.
            var work = WorldActivityFactory.Create(WorldActivityType.Work, CreateContext(registry),
                pointTag: SmartPointTags.TradeStall);
            work.Enter(brain);
            work.Tick(brain, 0.1f);

            Assert.AreEqual(SmartPointTags.TradeStall, registry.LastRequestedTag);
            Assert.AreEqual(stall.Position, agent.LastDestination, "walks to the claimed stall");
        }

        [TestMethod]
        public void WorkWithoutDataClaimsTheForge()
        {
            var registry = new FakeRegistry(new FakePoint(SmartPointTags.Forge, new Vector2(150, 0)));
            var brain = CreateBrain(new FakeAgent(), registry);

            var work = WorldActivityFactory.Create(WorldActivityType.Work, CreateContext(registry));
            work.Enter(brain);

            Assert.AreEqual(SmartPointTags.Forge, registry.LastRequestedTag);
        }

        [TestMethod]
        public void WorkPosesAtThePointWithoutDampeningTheSenses()
        {
            var forge = new FakePoint(SmartPointTags.Forge, new Vector2(150, 0));
            var registry = new FakeRegistry(forge);
            var agent = new FakeAgent { Position = forge.Position }; // already standing at the anvil
            var brain = CreateBrain(agent, registry);

            var work = WorldActivityFactory.Create(WorldActivityType.Work, CreateContext(registry));
            work.Enter(brain);
            work.Tick(brain, 0.1f);

            Assert.AreEqual(ActivityPoses.Work, agent.Pose);
            Assert.AreEqual(1f, brain.VisionMultiplier, "unlike Sleep, work keeps full senses");
            Assert.AreEqual(1f, brain.HearingMultiplier);
        }

        [TestMethod]
        public void ExitReleasesTheClaimedPoint()
        {
            var registry = new FakeRegistry(new FakePoint(SmartPointTags.Forge, Vector2.Zero));
            var brain = CreateBrain(new FakeAgent(), registry);

            var work = WorldActivityFactory.Create(WorldActivityType.Work, CreateContext(registry));
            work.Enter(brain);
            work.Exit(brain);

            Assert.AreEqual(SelfId, registry.LastReleasedClaimant);
        }

        [TestMethod]
        public void WorkWithoutASmartPointRegistryDegradesToStayingHome()
        {
            var agent = new FakeAgent { Position = new Vector2(500, 0) };
            var brain = new WorldBrain(agent, CreateConfig(), new DefaultRandomNumberGenerator(seed: 42));

            var work = WorldActivityFactory.Create(WorldActivityType.Work, new WorldActivityContext());
            work.Enter(brain);
            work.Tick(brain, 0.1f);

            Assert.AreEqual(agent.HomePosition, agent.LastDestination, "no registry → falls back home");
        }

        // ---- helpers ----

        private const string SelfId = "npc-smith";

        private static WorldBrainConfig CreateConfig() => new()
        {
            Activity = WorldActivityType.Idle, // the brain's own activity must not touch the assertions
        };

        private static WorldBrain CreateBrain(FakeAgent agent, FakeRegistry registry) =>
            new(agent, CreateConfig(), new DefaultRandomNumberGenerator(seed: 42), CreateContext(registry));

        private static WorldActivityContext CreateContext(FakeRegistry registry)
        {
            var self = new Mock<ISkirmishParticipant>();
            self.Setup(participant => participant.InstanceId).Returns(SelfId);
            return new WorldActivityContext { Points = registry, Self = self.Object };
        }

        private class FakePoint(string tag, Vector2 position, int capacity = 1) : ISmartPoint
        {
            public string Tag => tag;
            public int Capacity => capacity;
            public Vector2 Position => position;
        }

        private class FakeRegistry(params ISmartPoint[] points) : ISmartPointRegistry
        {
            private readonly List<ISmartPoint> _points = [.. points];

            public string? LastRequestedTag { get; private set; }
            public string? LastReleasedClaimant { get; private set; }

            public void Register(ISmartPoint point) => _points.Add(point);

            public void Unregister(ISmartPoint point) => _points.Remove(point);

            public ISmartPoint? TryClaim(string tag, string claimantId, Vector2 from)
            {
                LastRequestedTag = tag;
                return _points.Find(point => point.Tag == tag);
            }

            public void Release(string claimantId) => LastReleasedClaimant = claimantId;
        }

        private class FakeAgent : IWorldAgent
        {
            public Vector2 Position { get; set; } = Vector2.Zero;
            public Vector2 HomePosition { get; set; } = Vector2.Zero;
            public bool IsFighting { get; set; }
            public Vector2? LastDestination { get; private set; }
            public string? Pose { get; private set; }

            public void MoveTo(Vector2 destination, float speed) => LastDestination = destination;

            public void StopMoving() => LastDestination = null;

            public TargetSighting? GetSighting(float visionRadius) => null;

            public void SetActivityPose(string clip) => Pose = clip;

            public void ClearActivityPose() => Pose = null;
        }
    }
}
