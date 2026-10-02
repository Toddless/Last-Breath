namespace LastBreathTest.GameData
{
    using Core.Data.GameData;

    [TestClass]
    public class GameDataServiceTests
    {
        private sealed class FakeSource : IGameDataSource
        {
            public Dictionary<string, List<GameDataFile>> Catalogs { get; } = [];

            public IReadOnlyList<GameDataFile> ReadCatalog(string catalog) =>
                Catalogs.TryGetValue(catalog, out var files)
                    ? files
                    : throw new DirectoryNotFoundException($"Data catalog '{catalog}' does not exist");
        }

        private sealed class FakeParticipant(params string[] catalogs) : IGameDataParticipant
        {
            public List<(string Catalog, string FileName, string Json)> Applied { get; } = [];
            public Func<GameDataFile, bool>? ShouldThrow { get; init; }

            public IReadOnlyList<string> Catalogs => catalogs;

            public void Apply(string catalog, GameDataFile file)
            {
                if (ShouldThrow?.Invoke(file) == true) throw new FormatException("broken file");
                Applied.Add((catalog, file.FileName, file.Json));
            }
        }

        private FakeSource _source = null!;

        [TestInitialize]
        public void Setup() => _source = new FakeSource();

        [TestMethod]
        public void LoadAllFeedsEveryFileOfEveryDeclaredCatalog()
        {
            _source.Catalogs["Npc"] = [new GameDataFile("Npc.json", "{}"), new GameDataFile("More.json", "{}")];
            _source.Catalogs["NpcBehaviors"] = [new GameDataFile("Behavior.json", "{}")];
            var participant = new FakeParticipant("Npc", "NpcBehaviors");

            new GameDataService(_source, [participant]).LoadAll();

            Assert.AreEqual(3, participant.Applied.Count);
            Assert.AreEqual(("Npc", "Npc.json", "{}"), participant.Applied[0]);
            Assert.AreEqual(("NpcBehaviors", "Behavior.json", "{}"), participant.Applied[2]);
        }

        [TestMethod]
        public void BrokenFileIsReportedAndSkippedRestOfCatalogLoads()
        {
            _source.Catalogs["Factions"] = [new GameDataFile("Bad.json", "oops"), new GameDataFile("Good.json", "{}")];
            var participant = new FakeParticipant("Factions") { ShouldThrow = file => file.FileName == "Bad.json" };
            var service = new GameDataService(_source, [participant]);
            var failures = new List<string>();
            service.LoadFailed += (context, _) => failures.Add(context);

            service.LoadAll();

            Assert.AreEqual(1, participant.Applied.Count);
            Assert.AreEqual("Good.json", participant.Applied[0].FileName);
            CollectionAssert.AreEqual(new[] { "Factions/Bad.json" }, failures);
        }

        [TestMethod]
        public void MissingCatalogIsReportedAndOtherCatalogsStillLoad()
        {
            _source.Catalogs["World"] = [new GameDataFile("WorldClock.json", "{}")];
            var participant = new FakeParticipant("Player", "World");
            var service = new GameDataService(_source, [participant]);
            var failures = new List<string>();
            service.LoadFailed += (context, _) => failures.Add(context);

            service.LoadAll();

            Assert.AreEqual(1, participant.Applied.Count);
            Assert.AreEqual("WorldClock.json", participant.Applied[0].FileName);
            CollectionAssert.AreEqual(new[] { "Player" }, failures);
        }

        [TestMethod]
        public void ParticipantsAreIndependentOneFailingDoesNotStopOthers()
        {
            _source.Catalogs["NpcBuffs"] = [new GameDataFile("Buffs.json", "{}")];
            var broken = new FakeParticipant("NpcBuffs") { ShouldThrow = _ => true };
            var healthy = new FakeParticipant("NpcBuffs");

            new GameDataService(_source, [broken, healthy]).LoadAll();

            Assert.AreEqual(0, broken.Applied.Count);
            Assert.AreEqual(1, healthy.Applied.Count);
        }
    }

    [TestClass]
    public class FileSystemDataSourceTests
    {
        private string _root = null!;

        [TestInitialize]
        public void Setup() => _root = Directory.CreateTempSubdirectory("gamedata_").FullName;

        [TestCleanup]
        public void Cleanup() => Directory.Delete(_root, recursive: true);

        [TestMethod]
        public void ReadsJsonFilesIncludingNestedFoldersAndIgnoresOtherExtensions()
        {
            string catalog = Path.Combine(_root, "EquipItems");
            Directory.CreateDirectory(Path.Combine(catalog, "Weapons"));
            File.WriteAllText(Path.Combine(catalog, "Amulet.json"), "{\"a\":1}");
            File.WriteAllText(Path.Combine(catalog, "Weapons", "Sword.json"), "{\"s\":2}");
            File.WriteAllText(Path.Combine(catalog, "notes.txt"), "not data");

            var files = new FileSystemDataSource(_root).ReadCatalog("EquipItems");

            Assert.AreEqual(2, files.Count);
            Assert.IsTrue(files.Any(f => f.FileName == "Amulet.json" && f.Json == "{\"a\":1}"));
            Assert.IsTrue(files.Any(f => f.FileName == "Sword.json" && f.Json == "{\"s\":2}"));
        }

        [TestMethod]
        public void MissingCatalogThrowsDirectoryNotFound()
        {
            var source = new FileSystemDataSource(_root);
            Assert.ThrowsException<DirectoryNotFoundException>(() => source.ReadCatalog("Nope"));
        }
    }

    [TestClass]
    public class CompositeDataSourceTests
    {
        private sealed class StubSource(Dictionary<string, List<GameDataFile>> catalogs) : IGameDataSource
        {
            public IReadOnlyList<GameDataFile> ReadCatalog(string catalog) =>
                catalogs.TryGetValue(catalog, out var files)
                    ? files
                    : throw new DirectoryNotFoundException(catalog);
        }

        [TestMethod]
        public void CatalogIsServedWhollyByFirstSourceThatHasIt()
        {
            var local = new StubSource(new() { ["Npc"] = [new GameDataFile("Local.json", "local")] });
            var shared = new StubSource(new() { ["Npc"] = [new GameDataFile("Shared.json", "shared")] });

            var files = new CompositeDataSource([local, shared]).ReadCatalog("Npc");

            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("Local.json", files[0].FileName);
        }

        [TestMethod]
        public void FallsThroughToNextSourceWhenCatalogMissing()
        {
            var local = new StubSource([]);
            var shared = new StubSource(new() { ["Factions"] = [new GameDataFile("FactionRelations.json", "{}")] });

            var files = new CompositeDataSource([local, shared]).ReadCatalog("Factions");

            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("FactionRelations.json", files[0].FileName);
        }

        [TestMethod]
        public void MissingInAllSourcesThrows()
        {
            var composite = new CompositeDataSource([new StubSource([]), new StubSource([])]);
            Assert.ThrowsException<DirectoryNotFoundException>(() => composite.ReadCatalog("Nope"));
        }
    }

    [TestClass]
    public class EnumParserTests
    {
        private enum Sample { None, First, Second }

        [TestMethod]
        public void ParseEnumIsCaseInsensitive() => Assert.AreEqual(Sample.Second, Core.Data.EnumParser.ParseEnum<Sample>("second"));

        [TestMethod]
        public void ParseEnumThrowsOnTypoInsteadOfSilentDefault() =>
            Assert.ThrowsException<FormatException>(() => Core.Data.EnumParser.ParseEnum<Sample>("Secnod"));

        [TestMethod]
        public void ParseEnumOrDefaultTreatsAbsentAsDefaultButParsesPresentStrictly()
        {
            Assert.AreEqual(Sample.None, Core.Data.EnumParser.ParseEnumOrDefault<Sample>(null));
            Assert.AreEqual(Sample.None, Core.Data.EnumParser.ParseEnumOrDefault<Sample>(""));
            Assert.AreEqual(Sample.First, Core.Data.EnumParser.ParseEnumOrDefault<Sample>("first"));
            Assert.ThrowsException<FormatException>(() => Core.Data.EnumParser.ParseEnumOrDefault<Sample>("frist"));
        }
    }
}
