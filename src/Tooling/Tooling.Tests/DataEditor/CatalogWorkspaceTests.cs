namespace Tooling.Tests.DataEditor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Tooling.Catalogs;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// What the data editor is handed before it draws anything: the files of a catalog folder, and the
    /// records inside them found by the shape the catalog is written in.
    /// <para>The four shapes are exercised on hand-written schemas rather than on the game's own DTOs —
    /// the walk is driven by <see cref="RootShape"/> and by <see cref="RecordSchema.IdField"/>, and a
    /// test that went through reflection would be pinning the reflector a second time.</para>
    /// <para>Everything a folder can be wrong about is a note and never an exception: the tool has to
    /// open on the catalogs that do read.</para>
    /// </summary>
    [TestClass]
    public class CatalogWorkspaceTests
    {
        private const string NpcCatalog = "Npc";
        private const string TablesCatalog = "LootTables";
        private const string MapCatalog = "Formatting";
        private const string SettingsCatalog = "CombatRules";

        private const string NpcsKey = "npcs";
        private const string GeneralKey = "general";
        private const string IndividualKey = "individual";
        private const string RootKey = "";

        private const string IdField = "id";
        private const string NameField = "name";

        private const string RonaldId = "Npc_Ronald";
        private const string SkeletonId = "Npc_Skeleton";
        private const string FirstTableId = "Table_General";
        private const string IndividualTableId = "Table_Ronald";
        private const string HealthKey = "Health";
        private const string ManaKey = "Mana";

        private const string NpcFile = "Npc.json";
        private const string ExtraFile = "Npc_Extra.json";
        private const string NestedFolder = "Bandits";
        private const string NotJsonFile = "notes.txt";

        private const string TwoNpcs = """
            {
                "npcs": [
                    { "id": "Npc_Ronald", "name": "Ronald" },
                    { "id": "Npc_Skeleton", "name": "Skeleton" }
                ]
            }
            """;

        /// <summary>The second record carries no id at all; the first carries one that is not a string.</summary>
        private const string NamelessNpcs = """
            {
                "npcs": [
                    { "id": 12 },
                    { "name": "no id here" }
                ]
            }
            """;

        private const string OneNpc = """
            {
                "npcs": [
                    { "id": "Npc_Bandit" }
                ]
            }
            """;

        private const string SectionsOfTables = """
            {
                "general": [
                    { "id": "Table_General" }
                ],
                "individual": [
                    { "id": "Table_Ronald" }
                ]
            }
            """;

        private const string MapOfRecords = """
            {
                "Health": { "name": "health" },
                "Mana": { "name": "mana" }
            }
            """;

        private const string OneRecord = """
            { "id": "Rules", "name": "combat" }
            """;

        private const string NpcsNotAnArray = """
            { "npcs": { "id": "Npc_Ronald" } }
            """;

        private const string NotJson = "{ this is not json";

        private string _root = string.Empty;

        [TestInitialize]
        public void CreateTheRoot()
        {
            _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TestCleanup]
        public void DeleteTheRoot()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        [TestMethod]
        public void FilePaths_ReadsNestedFoldersAndOnlyJson()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);
            Write(NpcCatalog, NotJsonFile, NotJson);
            Write(Path.Combine(NpcCatalog, NestedFolder), ExtraFile, OneNpc);

            IReadOnlyList<string> paths = CatalogWorkspace.FilePaths(CatalogWorkspace.Folder(_root, NpcCatalog));

            CollectionAssert.AreEqual(
                new[] { Path.Combine(_root, NpcCatalog, NestedFolder, ExtraFile), Path.Combine(_root, NpcCatalog, NpcFile) },
                paths.ToArray());
        }

        [TestMethod]
        public void FilePaths_AnswersNothingForAFolderThatIsNotThere()
        {
            Assert.AreEqual(0, CatalogWorkspace.FilePaths(CatalogWorkspace.Folder(_root, NpcCatalog)).Count);
        }

        [TestMethod]
        public void Load_ReadsTheRecordsOfAnArrayUnderAKey()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Single(Load(Npcs()));

            CollectionAssert.AreEqual(new[] { RonaldId, SkeletonId }, view.Records.Select(record => record.Id).ToArray());
            Assert.AreEqual("/npcs/1", view.Records[1].Pointer.ToString());
            Assert.AreEqual(NpcFile, view.Records[1].File.Name);
            Assert.AreEqual(1, view.Files.Count);
        }

        [TestMethod]
        public void Load_ReadsTheValueAtTheRecordsAddress()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogRecord record = Single(Load(Npcs())).Records[0];

            Assert.AreEqual(RonaldId, record.Token?[IdField]?.ToString());
        }

        [TestMethod]
        public void Load_NamesARecordWithoutAnIdByItsPlace()
        {
            Write(NpcCatalog, NpcFile, NamelessNpcs);

            CatalogView view = Single(Load(Npcs()));

            // A number written under the id field is still an id; nothing written there at all is not.
            CollectionAssert.AreEqual(new[] { "12", "#1" }, view.Records.Select(record => record.Id).ToArray());
        }

        [TestMethod]
        public void Load_ReadsEveryFileOfTheCatalogInOneList()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);
            Write(Path.Combine(NpcCatalog, NestedFolder), ExtraFile, OneNpc);

            CatalogView view = Single(Load(Npcs()));

            Assert.AreEqual(2, view.Files.Count);
            Assert.AreEqual(3, view.Records.Count);
            Assert.AreEqual(ExtraFile, view.Records[0].File.Name);
        }

        [TestMethod]
        public void Load_ReadsTheRecordsOfEverySection()
        {
            Write(TablesCatalog, NpcFile, SectionsOfTables);

            CatalogView view = Single(Load(Tables()));

            CollectionAssert.AreEqual(
                new[] { FirstTableId, IndividualTableId },
                view.Records.Select(record => record.Id).ToArray());
            Assert.AreEqual("/individual/0", view.Records[1].Pointer.ToString());
        }

        [TestMethod]
        public void Load_NamesTheRecordsOfAMapByTheirKey()
        {
            Write(MapCatalog, NpcFile, MapOfRecords);

            CatalogView view = Single(Load(Map()));

            CollectionAssert.AreEqual(new[] { HealthKey, ManaKey }, view.Records.Select(record => record.Id).ToArray());
            Assert.AreEqual("/Mana", view.Records[1].Pointer.ToString());
        }

        [TestMethod]
        public void Load_ReadsADocumentThatIsOneRecord()
        {
            Write(SettingsCatalog, NpcFile, OneRecord);

            CatalogView view = Single(Load(Settings()));

            Assert.AreEqual(1, view.Records.Count);
            Assert.IsTrue(view.Records[0].Pointer.IsRoot);
            Assert.AreEqual("Rules", view.Records[0].Id);
        }

        [TestMethod]
        public void Load_NotesAFolderThatIsNotThereAndKeepsTheCatalog()
        {
            CatalogView view = Single(Load(Npcs()));

            Assert.AreEqual(0, view.Records.Count);
            Assert.AreEqual(1, view.Notes.Count);
            StringAssert.Contains(view.Notes[0], NpcCatalog);
        }

        [TestMethod]
        public void Load_NotesAFolderWithNoFilesInIt()
        {
            Directory.CreateDirectory(CatalogWorkspace.Folder(_root, NpcCatalog));

            CatalogView view = Single(Load(Npcs()));

            Assert.AreEqual(1, view.Notes.Count);
            StringAssert.Contains(view.Notes[0], CatalogWorkspace.FileExtension);
        }

        [TestMethod]
        public void Load_NotesAFileThatIsNotJsonAndReadsTheRest()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);
            Write(NpcCatalog, ExtraFile, NotJson);

            CatalogView view = Single(Load(Npcs()));

            Assert.AreEqual(1, view.Files.Count);
            Assert.AreEqual(2, view.Records.Count);
            Assert.AreEqual(1, view.Notes.Count);
            StringAssert.Contains(view.Notes[0], ExtraFile);
        }

        [TestMethod]
        public void Load_NotesASectionThatHoldsNoArray()
        {
            Write(NpcCatalog, NpcFile, NpcsNotAnArray);

            CatalogView view = Single(Load(Npcs()));

            Assert.AreEqual(0, view.Records.Count);
            Assert.AreEqual(1, view.Notes.Count);
            StringAssert.Contains(view.Notes[0], NpcsKey);
        }

        [TestMethod]
        public void Load_NamesTheCatalogOnEveryNoteOfTheReport()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogWorkspace workspace = Load(Npcs(), Tables());

            Assert.AreEqual(1, workspace.Report.Count);
            StringAssert.StartsWith(workspace.Report[0], TablesCatalog);
        }

        [TestMethod]
        public void Load_CountsTheFilesAndRecordsOfEveryCatalog()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);
            Write(TablesCatalog, NpcFile, SectionsOfTables);

            CatalogWorkspace workspace = Load(Npcs(), Tables());

            Assert.AreEqual(_root, workspace.Root);
            Assert.AreEqual(2, workspace.Catalogs.Count);
            Assert.AreEqual(2, workspace.FileCount);
            Assert.AreEqual(4, workspace.RecordCount);
        }

        private CatalogWorkspace Load(params ICatalogDescriptor[] descriptors) =>
            CatalogWorkspace.Load(_root, descriptors);

        private static CatalogView Single(CatalogWorkspace workspace)
        {
            Assert.AreEqual(1, workspace.Catalogs.Count);
            return workspace.Catalogs[0];
        }

        private void Write(string catalog, string file, string content)
        {
            string folder = Path.Combine(_root, catalog);

            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, file), content);
        }

        private static ICatalogDescriptor Npcs() => new Descriptor(
            NpcCatalog,
            RootShape.ArrayUnderKey,
            [Section(NpcsKey, Record(IdField))]);

        private static ICatalogDescriptor Tables() => new Descriptor(
            TablesCatalog,
            RootShape.SectionsOfArrays,
            [Section(GeneralKey, Record(IdField)), Section(IndividualKey, Record(IdField))]);

        private static ICatalogDescriptor Map() => new Descriptor(
            MapCatalog,
            RootShape.Dictionary,
            [Section(RootKey, Record(idField: null))]);

        private static ICatalogDescriptor Settings() => new Descriptor(
            SettingsCatalog,
            RootShape.Single,
            [Section(RootKey, Record(IdField))]);

        private static SectionSchema Section(string key, RecordSchema record) => new() { Key = key, Record = record };

        private static RecordSchema Record(string? idField) => new()
        {
            TypeName = nameof(Record),
            Fields =
            [
                new FieldSchema { JsonName = IdField, Kind = FieldKind.String },
                new FieldSchema { JsonName = NameField, Kind = FieldKind.String }
            ],
            IdField = idField
        };

        /// <summary>A catalog described in full by hand: the builder is offered and not used, which is
        /// what keeps the shapes under test out of the reflector's reach.</summary>
        private sealed class Descriptor(string catalog, RootShape shape, SchemaList<SectionSchema> sections)
            : ICatalogDescriptor
        {
            public string Catalog => catalog;

            public CatalogSchema Describe(ISchemaBuilder builder) =>
                new(shape, sections, [], new SingleFilePlacement { FileName = catalog });
        }
    }
}
