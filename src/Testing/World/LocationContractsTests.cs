namespace LastBreathTest.World
{
    using Core.Ai.World.Time;
    using Core.Data.GameData;
    using Core.Save;
    using Core.Save.Participants;
    using Core.World.Locations;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class LocationContractsTests
    {
        private static LocationCatalogData Graph() => new()
        {
            Start = new("MainWorld", "Start"),
            Locations = [new("MainWorld", "main.tscn"), new("Cave", "cave.tscn")],
            Connections = [new(new("MainWorld", "Door"), new("Cave", "Entry")), new(new("Cave", "Entry"), new("MainWorld", "Door"))]
        };

        [TestMethod]
        public void ConnectionsAreExplicitAndDirectional()
        {
            var catalog = new LocationCatalog();
            catalog.Apply("Locations", new GameDataFile("Locations.json", JsonConvert.SerializeObject(Graph())));
            Assert.AreEqual(new LocationAddress("Cave", "Entry"), catalog.Destination(new("MainWorld", "Door")));
            Assert.AreEqual(new LocationAddress("MainWorld", "Door"), catalog.Destination(new("Cave", "Entry")));
            Assert.IsNull(catalog.Destination(new("MainWorld", "Start")));
        }

        [TestMethod]
        public void InvalidConnectionsCannotReplaceTheCatalog()
        {
            var graph = Graph();
            graph.Connections.Add(new(new("MainWorld", "Door"), new("Cave", "Exit")));
            Assert.ThrowsException<InvalidOperationException>(() => LocationCatalog.Validate(graph));
            graph = Graph();
            graph.Connections.Add(new(new("Unknown", "Door"), new("Cave", "Entry")));
            Assert.ThrowsException<InvalidOperationException>(() => LocationCatalog.Validate(graph));
        }

        [TestMethod]
        public void ClockSavePreservesFractionsAndLegacyMinutes()
        {
            var clock = new WorldClock();
            clock.Tick(0.125f);
            double saved = clock.TotalMinutes;
            var participant = new WorldClockSaveParticipant(clock);
            var data = participant.Capture();
            clock.Tick(10);
            participant.Restore(data, participant.Version);
            Assert.AreEqual(saved, clock.TotalMinutes, 0.00000001);
            participant.Restore(new JObject { ["day"] = 2, ["minuteOfDay"] = 23 }, 1);
            Assert.AreEqual(2 * 1440.0 + 23, clock.TotalMinutes);
        }

        [TestMethod]
        public void SnapshotsUseOnlyElapsedGameTime()
        {
            var snapshot = new LocationSnapshot { LastSimulatedAt = 25.125 };
            Assert.AreEqual(0.375, snapshot.ElapsedMinutes(25.5));
            Assert.AreEqual(0, snapshot.ElapsedMinutes(25));
            var restored = JsonConvert.DeserializeObject<LocationSnapshot>(JsonConvert.SerializeObject(snapshot))!;
            Assert.AreEqual(0, restored.ElapsedMinutes(25.125));
        }

        [TestMethod]
        public void NestedRestorationKeepsTheOuterSideEffectGate()
        {
            var scope = new LoadScope();
            using (scope.Begin())
            {
                using (scope.Begin()) Assert.IsTrue(scope.IsLoading);
                Assert.IsTrue(scope.IsLoading);
            }
            Assert.IsFalse(scope.IsLoading);
        }
    }
}
