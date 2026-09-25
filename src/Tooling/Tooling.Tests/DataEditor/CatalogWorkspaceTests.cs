namespace Tooling.Tests.DataEditor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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

        /// <summary>What a describer that will not describe says, so the note can be shown to carry it.</summary>
        private const string BrokenMessage = "this describer refuses";

        /// <summary>A culture that writes a fraction with a comma and is not the one the tests run in:
        /// the values a file holds are the file's, and a machine set to it must read them the same.</summary>
        private const string OtherCulture = "ru-RU";

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

        /// <summary>The same id as <see cref="TwoNpcs"/> writes, in a file of its own.</summary>
        private const string RonaldAgain = """
            {
                "npcs": [
                    { "id": "Npc_Ronald", "name": "Ronald of the second file" }
                ]
            }
            """;

        /// <summary>Ids of every scalar kind json has but text: a fraction, a flag, and an integer too
        /// long to be held as anything but a long.</summary>
        private const string UntypedIds = """
            {
                "npcs": [
                    { "id": 0.5 },
                    { "id": true },
                    { "id": 9007199254740993 }
                ]
            }
            """;

        private const string NpcsNotAnArray = """
            { "npcs": { "id": "Npc_Ronald" } }
            """;

        /// <summary>A document written as a list where the shape of its catalog says one record, or a
        /// map of them.</summary>
        private const string AnArray = "[ ]";

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
        public void Load_NamesARecordWhoseIdIsNotTextTheWayTheFileWritesIt()
        {
            Write(NpcCatalog, NpcFile, UntypedIds);

            CultureInfo spoken = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(OtherCulture);

            try
            {
                // The premise of the test: in this culture the CLR's own way of writing a number is not
                // the file's. Without it the test would pass on any machine and prove nothing.
                Assert.AreNotEqual("0.5", 0.5.ToString(CultureInfo.CurrentCulture));

                CatalogView view = Single(Load(Npcs()));

                CollectionAssert.AreEqual(
                    new[] { "0.5", "true", "9007199254740993" },
                    view.Records.Select(record => record.Id).ToArray());
            }
            finally
            {
                CultureInfo.CurrentCulture = spoken;
            }
        }

        [TestMethod]
        public void Load_KeepsBothRecordsWhenTwoFilesWriteOneId()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);
            Write(NpcCatalog, ExtraFile, RonaldAgain);

            CatalogView view = Single(Load(Npcs()));

            CatalogRecord[] ronalds = [.. view.Records.Where(record => record.Id == RonaldId)];

            Assert.AreEqual(3, view.Records.Count);
            Assert.AreEqual(2, ronalds.Length);
            CollectionAssert.AreEquivalent(
                new[] { NpcFile, ExtraFile },
                ronalds.Select(record => record.File.Name).ToArray());
            Assert.AreEqual(0, view.Notes.Count);
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
        public void Load_NotesADocumentThatIsNotOneRecord()
        {
            Write(SettingsCatalog, NpcFile, AnArray);

            CatalogView view = Single(Load(Settings()));

            Assert.AreEqual(0, view.Records.Count);
            Assert.AreEqual(1, view.Notes.Count);
            StringAssert.Contains(view.Notes[0], NpcFile);
        }

        [TestMethod]
        public void Load_NotesASectionThatHoldsNoMap()
        {
            Write(MapCatalog, NpcFile, AnArray);

            CatalogView view = Single(Load(Map()));

            Assert.AreEqual(0, view.Records.Count);
            Assert.AreEqual(1, view.Notes.Count);
            StringAssert.Contains(view.Notes[0], NpcFile);
        }

        [TestMethod]
        public void Load_NotesADescriberThatThrowsAndReadsTheCatalogsThatDid()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogWorkspace workspace = Load(new BrokenDescriptor(TablesCatalog), Npcs());

            Assert.AreEqual(1, workspace.Catalogs.Count);
            Assert.AreEqual(NpcCatalog, workspace.Catalogs[0].Catalog);
            Assert.AreEqual(2, workspace.RecordCount);
            Assert.AreEqual(1, workspace.Report.Count);
            StringAssert.StartsWith(workspace.Report[0], TablesCatalog);
            StringAssert.Contains(workspace.Report[0], BrokenMessage);
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

        private void Write(string catalog, string file, string content) =>
            CatalogFixture.Write(_root, catalog, file, content);

        private static ICatalogDescriptor Npcs() => CatalogFixture.Descriptor(
            NpcCatalog,
            RootShape.ArrayUnderKey,
            [CatalogFixture.Section(NpcsKey, Record(IdField))]);

        private static ICatalogDescriptor Tables() => CatalogFixture.Descriptor(
            TablesCatalog,
            RootShape.SectionsOfArrays,
            [CatalogFixture.Section(GeneralKey, Record(IdField)), CatalogFixture.Section(IndividualKey, Record(IdField))]);

        private static ICatalogDescriptor Map() => CatalogFixture.Descriptor(
            MapCatalog,
            RootShape.Dictionary,
            [CatalogFixture.Section(RootKey, Record(idField: null))]);

        private static ICatalogDescriptor Settings() => CatalogFixture.Descriptor(
            SettingsCatalog,
            RootShape.Single,
            [CatalogFixture.Section(RootKey, Record(IdField))]);

        private static RecordSchema Record(string? idField) => CatalogFixture.Record(
            idField,
            CatalogFixture.Field(IdField, FieldKind.String),
            CatalogFixture.Field(NameField, FieldKind.String));

        /// <summary>A describer that throws something the library was never told to expect. A descriptor
        /// is code from outside it, and one catalog refusing to be described has to cost that catalog
        /// and nothing else.</summary>
        private sealed class BrokenDescriptor(string catalog) : ICatalogDescriptor
        {
            public string Catalog => catalog;

            public CatalogSchema Describe(ISchemaBuilder builder) => throw new InvalidOperationException(BrokenMessage);
        }
    }
}
