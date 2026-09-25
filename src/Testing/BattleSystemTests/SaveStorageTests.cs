namespace LastBreathTest.BattleSystemTests
{
    using Core.Save;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class SaveStorageTests
    {
        private string _root = null!;
        private SaveStorage _storage = null!;

        [TestInitialize]
        public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), $"lastbreath_saves_{Guid.NewGuid():N}");
            _storage = new SaveStorage(_root);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [TestMethod]
        public void WriteThenLoadRoundTrips()
        {
            _storage.Write(3, FileWithLevel(7, name: "Todd"));

            var loaded = _storage.Load(3);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Todd", loaded.Metadata.Name);
            Assert.AreEqual(7, (int)loaded.Sections["mastery"].Data["level"]!);
        }

        [TestMethod]
        public void LoadMissingSlotReturnsNull()
        {
            Assert.IsNull(_storage.Load(1));
            Assert.IsFalse(_storage.Exists(1));
        }

        [TestMethod]
        public void SecondWriteKeepsPreviousGenerationAsBackup()
        {
            _storage.Write(1, FileWithLevel(1));
            _storage.Write(1, FileWithLevel(2));

            Assert.IsTrue(File.Exists(Path.Combine(_root, "slot_1", "save.json.bak")));
            Assert.AreEqual(2, (int)_storage.Load(1)!.Sections["mastery"].Data["level"]!);
        }

        [TestMethod]
        public void CorruptedMainFileFallsBackToBackup()
        {
            _storage.Write(1, FileWithLevel(1));
            _storage.Write(1, FileWithLevel(2));
            File.WriteAllText(Path.Combine(_root, "slot_1", "save.json"), "{ not valid json !!");

            var loaded = _storage.Load(1);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, (int)loaded.Sections["mastery"].Data["level"]!);
        }

        [TestMethod]
        public void ReadMetadataUsesMetaFile()
        {
            _storage.Write(2, FileWithLevel(5, name: "Checkpoint"));

            var metadata = _storage.ReadMetadata(2);

            Assert.IsNotNull(metadata);
            Assert.AreEqual("Checkpoint", metadata.Name);
            Assert.IsTrue(File.Exists(Path.Combine(_root, "slot_2", "meta.json")));
        }

        [TestMethod]
        public void ReadMetadataFallsBackToSaveFileWhenMetaMissing()
        {
            _storage.Write(2, FileWithLevel(5, name: "Checkpoint"));
            File.Delete(Path.Combine(_root, "slot_2", "meta.json"));

            var metadata = _storage.ReadMetadata(2);

            Assert.IsNotNull(metadata);
            Assert.AreEqual("Checkpoint", metadata.Name);
        }

        [TestMethod]
        public void DeleteRemovesSlot()
        {
            _storage.Write(1, FileWithLevel(1));

            _storage.Delete(1);

            Assert.IsFalse(_storage.Exists(1));
            Assert.IsNull(_storage.Load(1));
        }

        private static SaveFile FileWithLevel(int level, string name = "Slot")
        {
            var file = new SaveFile { Metadata = new SaveMetadata { Name = name, SavedAtUtc = DateTime.UtcNow } };
            file.Sections["mastery"] = new SaveSection { Version = 1, Data = new JObject { ["level"] = level } };
            return file;
        }
    }
}
