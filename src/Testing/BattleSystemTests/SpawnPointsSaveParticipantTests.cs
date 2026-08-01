namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Save;
    using Core.Save.Participants;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Spawn points are the one section whose fresh-game state is not a singleton's to hand back: the
    /// points are scene nodes, and they skip their own on-ready fill while a load is pending. The
    /// restore is the only thing left that can populate them.
    /// </summary>
    [TestClass]
    public class SpawnPointsSaveParticipantTests
    {
        [TestMethod]
        public void AFileWithoutTheSectionFillsEveryPoint()
        {
            var point = new FakeSpawnPoint("camp_1");
            var manager = new SaveManager(new LoadScope());
            var failures = new List<string>();
            manager.Register(new SpawnPointsSaveParticipant(RegistryOf(point)));
            manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            manager.Restore(new SaveFile()); // a save taken before the points were persisted

            Assert.AreEqual(1, point.FreshFills, "the load landed in a world holding no spawn-point NPC at all");
            Assert.AreEqual(0, point.Restores);
            Assert.AreEqual(0, failures.Count);
        }

        [TestMethod]
        public void ADamagedSectionFillsEveryPointInsteadOfEmptyingTheWorld()
        {
            var point = new FakeSpawnPoint("camp_1");
            var manager = new SaveManager(new LoadScope());
            var failures = new List<string>();
            manager.Register(new SpawnPointsSaveParticipant(RegistryOf(point)));
            manager.SectionRestoreFailed += (section, _) => failures.Add(section);

            var file = new SaveFile();
            file.Sections["spawnPoints"] = new SaveSection { Version = 1, Data = new JObject { ["Points"] = "corrupt" } };
            manager.Restore(file);

            Assert.AreEqual(1, point.FreshFills, "the load landed in a world holding no spawn-point NPC at all");
            CollectionAssert.AreEqual(new[] { "spawnPoints" }, failures, "the damage has to reach the log even so");
        }

        [TestMethod]
        public void ASavedPointResumesItsCountWhileANewOneFills()
        {
            var saved = new FakeSpawnPoint("camp_1") { Alive = 2 };
            var added = new FakeSpawnPoint("camp_2"); // placed in the scene after the save was taken

            JToken captured = new SpawnPointsSaveParticipant(RegistryOf(saved)).Capture();
            new SpawnPointsSaveParticipant(RegistryOf(saved, added)).Restore(captured, savedVersion: 1);

            Assert.AreEqual(2, saved.RestoredAlive);
            Assert.AreEqual(0, saved.FreshFills, "a saved camp refilling to capacity is the save-scum hole");
            Assert.AreEqual(1, added.FreshFills);
        }

        private static ISpawnPointRegistry RegistryOf(params IPersistentSpawnPoint[] points)
        {
            var registry = new Core.Services.SpawnPointRegistry();
            foreach (var point in points) registry.Register(point);
            return registry;
        }

        private sealed class FakeSpawnPoint(string id) : IPersistentSpawnPoint
        {
            public int Alive { get; init; }
            public int FreshFills { get; private set; }
            public int Restores { get; private set; }
            public int RestoredAlive { get; private set; }

            public string PointId => id;

            public SpawnPointSaveData CaptureState() => new() { Id = id, Alive = Alive };

            public void RestoreState(SpawnPointSaveData data)
            {
                Restores++;
                RestoredAlive = data.Alive;
            }

            public void FillFresh() => FreshFills++;
        }
    }
}
