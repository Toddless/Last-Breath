namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.SmartPoints;
    using Godot;

    [TestClass]
    public class SmartPointRegistryTests
    {
        [TestMethod]
        public void ClaimsTheNearestFreePointOfTheTag()
        {
            var registry = new SmartPointRegistry(new LastBreathTest.WorldTesting.TestSpatialQuery());
            var far = new FakePoint(SmartPointTags.Campfire, new Vector2(500, 0));
            var near = new FakePoint(SmartPointTags.Campfire, new Vector2(100, 0));
            var otherTag = new FakePoint(SmartPointTags.Tent, new Vector2(10, 0));
            registry.Register(far);
            registry.Register(near);
            registry.Register(otherTag);

            var claimed = registry.TryClaim(SmartPointTags.Campfire, "npc-1", Vector2.Zero);

            Assert.AreSame(near, claimed);
        }

        [TestMethod]
        public void CapacityIsRespectedAndReleaseFreesTheSeat()
        {
            var registry = new SmartPointRegistry(new LastBreathTest.WorldTesting.TestSpatialQuery());
            var point = new FakePoint(SmartPointTags.Campfire, Vector2.Zero);
            registry.Register(point);

            Assert.AreSame(point, registry.TryClaim(SmartPointTags.Campfire, "npc-1", Vector2.Zero));
            Assert.IsNull(registry.TryClaim(SmartPointTags.Campfire, "npc-2", Vector2.Zero), "capacity 1 is full");

            registry.Release("npc-1");
            Assert.AreSame(point, registry.TryClaim(SmartPointTags.Campfire, "npc-2", Vector2.Zero));
        }

        [TestMethod]
        public void ReclaimIsIdempotentForTheHolder()
        {
            var registry = new SmartPointRegistry(new LastBreathTest.WorldTesting.TestSpatialQuery());
            var point = new FakePoint(SmartPointTags.Campfire, Vector2.Zero);
            registry.Register(point);

            var first = registry.TryClaim(SmartPointTags.Campfire, "npc-1", Vector2.Zero);
            var again = registry.TryClaim(SmartPointTags.Campfire, "npc-1", new Vector2(999, 999));

            Assert.AreSame(first, again, "the holder keeps its point on a re-claim");
        }

        [TestMethod]
        public void UnregisterDropsTheDanglingClaims()
        {
            var registry = new SmartPointRegistry(new LastBreathTest.WorldTesting.TestSpatialQuery());
            var point = new FakePoint(SmartPointTags.OreVein, Vector2.Zero);
            var replacement = new FakePoint(SmartPointTags.OreVein, new Vector2(50, 0));
            registry.Register(point);
            registry.TryClaim(SmartPointTags.OreVein, "npc-1", Vector2.Zero);

            registry.Unregister(point); // the node left the scene
            registry.Register(replacement);

            Assert.AreSame(replacement, registry.TryClaim(SmartPointTags.OreVein, "npc-1", Vector2.Zero));
        }

        [TestMethod]
        public void DeathInsuranceReleasesByClaimantId()
        {
            var registry = new SmartPointRegistry(new LastBreathTest.WorldTesting.TestSpatialQuery());
            var point = new FakePoint(SmartPointTags.Tent, Vector2.Zero);
            registry.Register(point);
            registry.TryClaim(SmartPointTags.Tent, "npc-1", Vector2.Zero);

            registry.Release("npc-1"); // the DI wiring calls this on EntityDied/NpcFinalDeath

            Assert.AreSame(point, registry.TryClaim(SmartPointTags.Tent, "npc-2", Vector2.Zero));
        }

        private class FakePoint(string tag, Vector2 position, int capacity = 1) : ISmartPoint
        {
            public string Tag => tag;
            public int Capacity => capacity;
            public Vector2 Position => position;
        }
    }
}
