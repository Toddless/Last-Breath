namespace Tooling.Tests.DataEditor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// What happens to a catalog between an edit and the file on disk: which files a save touches, what
    /// they hold afterwards, and what a save that cannot write says instead of throwing.
    /// <para>The catalog is described by hand rather than reflected off a DTO — the saver is driven by
    /// the schema's key order and by each document's own history, and going through the reflector would
    /// be pinning it a second time.</para>
    /// </summary>
    [TestClass]
    public class CatalogSaverTests
    {
        private const string NpcCatalog = "Npc";
        private const string TablesCatalog = "LootTables";

        private const string NpcsKey = "npcs";
        private const string IdField = "id";
        private const string NameField = "name";
        private const string LevelField = "level";
        private const string RarityField = "rarity";
        private const string UnknownField = "mood";

        private const string NpcFile = "Npc.json";
        private const string ExtraFile = "Npc_Extra.json";

        private const string IdPointer = "/npcs/0/id";
        private const string NamePointer = "/npcs/0/name";
        private const string RarityPointer = "/npcs/0/rarity";
        private const string RecordPointer = "/npcs/0";

        private const string NewName = "Ronald the Elder";
        private const string FirstRarity = "Common";
        private const string SecondRarity = "Rare";

        /// <summary>A record already written the way canon writes it — four spaces, schema order, a
        /// newline at the end. Reading it and writing it back has to change not one byte, which is what
        /// makes it usable as the answer a round trip is measured against.</summary>
        private const string CanonicalNpc = """
            {
                "npcs": [
                    {
                        "id": "Npc_Ronald",
                        "name": "Ronald",
                        "level": 3
                    }
                ]
            }
            """;

        /// <summary>The same record with its keys in another order and one key no schema knows. Both
        /// halves matter: the declared keys have to come back in the declared order, and the key nobody
        /// declared has to still be there afterwards.</summary>
        private const string ShuffledNpc = """
            {
                "npcs": [
                    { "level": 3, "mood": "grim", "name": "Ronald", "id": "Npc_Ronald" }
                ]
            }
            """;

        /// <summary>A second file, written in a shape canon would not leave alone. A save that touched
        /// it would be visible as the shape changing.</summary>
        private const string SkeletonNpc = """
            { "npcs": [ { "id": "Npc_Skeleton" } ] }
            """;

        private const string OneTable = """
            { "npcs": [ { "id": "Table_General" } ] }
            """;

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
        public void IsDirty_FollowsTheHistoryOfTheFileAndNotAFlag()
        {
            Write(NpcCatalog, NpcFile, CanonicalNpc);

            CatalogView view = Npcs();

            Assert.IsFalse(CatalogSaver.IsDirty(view));

            Rename(view, NewName);
            Assert.IsTrue(CatalogSaver.IsDirty(view));

            // Back onto the state the file holds: dirty is a question about the tree, not a switch that
            // an edit throws and only a save can throw back.
            view.Files[0].Document.History.Undo();
            Assert.IsFalse(CatalogSaver.IsDirty(view));
        }

        [TestMethod]
        public void SaveDirty_WritesTheFilesThatChangedAndLeavesTheOthersAlone()
        {
            Write(NpcCatalog, NpcFile, CanonicalNpc);
            Write(NpcCatalog, ExtraFile, SkeletonNpc);

            CatalogView view = Npcs();
            Rename(view, NewName);

            CatalogSaveResult result = CatalogSaver.SaveDirty(view);

            CollectionAssert.AreEqual(new[] { Path.Combine(_root, NpcCatalog, NpcFile) }, result.Saved.ToArray());
            Assert.AreEqual(0, result.Notes.Count);

            // Untouched down to its shape: canon would have laid this record out over five lines.
            Assert.AreEqual(CatalogFixture.OnDisk(SkeletonNpc), Read(ExtraFile));
        }

        [TestMethod]
        public void SaveDirty_LeavesEveryFileItWroteClean()
        {
            Write(NpcCatalog, NpcFile, CanonicalNpc);

            CatalogView view = Npcs();
            Rename(view, NewName);

            CatalogSaver.SaveDirty(view);

            Assert.IsFalse(CatalogSaver.IsDirty(view));
            StringAssert.Contains(Read(NpcFile), NewName);
        }

        [TestMethod]
        public void SaveDirty_WritesTheDeclaredKeysInOrderAndKeepsTheOnesNobodyDeclared()
        {
            Write(NpcCatalog, NpcFile, ShuffledNpc);

            CatalogView view = Npcs();
            Rename(view, NewName);

            CatalogSaver.SaveDirty(view);

            string written = Read(NpcFile);

            CollectionAssert.AreEqual(
                new[] { IdField, NameField, LevelField, UnknownField },
                Keys(written).ToArray());
        }

        [TestMethod]
        public void SaveDirty_PutsTheFileBackByteForByteWhenTheEditsAreUndone()
        {
            Write(NpcCatalog, NpcFile, CanonicalNpc);

            string original = Read(NpcFile);
            CatalogView view = Npcs();
            JsonTreeDocument document = view.Files[0].Document;

            // A key the file does not hold yet, then a value over it: the two steps a field the author
            // has never filled in takes on its way to being written.
            Assert.IsTrue(document.Insert(JsonPointer.Parse(RecordPointer), RarityField, new JValue(FirstRarity)));
            Assert.IsTrue(document.SetValue(JsonPointer.Parse(RarityPointer), new JValue(SecondRarity)));

            CatalogSaver.SaveDirty(view);
            StringAssert.Contains(Read(NpcFile), RarityField);

            document.History.Undo();
            document.History.Undo();

            // Dirty again, and against the file rather than against the session: the state on disk is
            // the one that was saved, and the tree is no longer it.
            Assert.IsTrue(CatalogSaver.IsDirty(view));
            CatalogSaver.SaveDirty(view);

            Assert.AreEqual(original, Read(NpcFile));
        }

        [TestMethod]
        public void SaveDirty_NotesAFileItCouldNotWriteAndSavesTheRest()
        {
            Write(NpcCatalog, NpcFile, CanonicalNpc);
            Write(NpcCatalog, ExtraFile, SkeletonNpc);

            CatalogView view = Npcs();

            foreach (CatalogFile file in view.Files)
                Assert.IsTrue(file.Document.SetValue(JsonPointer.Parse(IdPointer), new JValue(NewName)));

            Block(NpcCatalog, ExtraFile);

            CatalogSaveResult result = CatalogSaver.SaveDirty(view);

            CollectionAssert.AreEqual(new[] { Path.Combine(_root, NpcCatalog, NpcFile) }, result.Saved.ToArray());
            Assert.AreEqual(1, result.Notes.Count);
            StringAssert.Contains(result.Notes[0], ExtraFile);

            // The refusal costs its own file and no more: the one that was written is clean, the one
            // that was not still has its change to be saved another time.
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
            Assert.IsFalse(view.Files[1].Document.History.IsClean);
        }

        [TestMethod]
        public void SaveAll_WritesEveryCatalogAndNamesTheCatalogOnEveryNote()
        {
            Write(NpcCatalog, NpcFile, CanonicalNpc);
            Write(TablesCatalog, NpcFile, OneTable);

            CatalogWorkspace workspace = Load();
            var saver = new CatalogSaver(workspace);

            foreach (CatalogView view in workspace.Catalogs)
                view.Files[0].Document.SetValue(JsonPointer.Parse(IdPointer), new JValue(NewName));

            Assert.IsTrue(saver.AnyDirty);
            Assert.AreEqual(2, saver.DirtyCount);

            Block(TablesCatalog, NpcFile);

            CatalogSaveResult result = saver.SaveAll();

            Assert.AreEqual(1, result.Saved.Count);
            Assert.AreEqual(1, result.Notes.Count);
            StringAssert.StartsWith(result.Notes[0], TablesCatalog);
        }

        [TestMethod]
        public void Changed_SpeaksForEveryFileOfTheRun()
        {
            Write(NpcCatalog, NpcFile, CanonicalNpc);

            CatalogWorkspace workspace = Load();
            var saver = new CatalogSaver(workspace);
            int heard = 0;

            saver.Changed += () => heard++;

            workspace.Catalogs[0].Files[0].Document.SetValue(JsonPointer.Parse(NamePointer), new JValue(NewName));
            Assert.AreEqual(1, heard);

            // A save is a change of what is unsaved, so it is heard too: what the host draws from this
            // is the mark on a name, and the mark has to come off.
            CatalogSaver.SaveDirty(workspace.Catalogs[0]);
            Assert.AreEqual(2, heard);
        }

        /// <summary>The keys of the one record of a written file, in the order the file writes them.</summary>
        private static IEnumerable<string> Keys(string written) =>
            ((JObject)JToken.Parse(written)[NpcsKey]![0]!).Properties().Select(property => property.Name);

        /// <summary>Gives the first record of the catalog another name, which is the smallest edit a
        /// record can take.</summary>
        private static void Rename(CatalogView view, string name) =>
            view.Files[0].Document.SetValue(JsonPointer.Parse(NamePointer), new JValue(name));

        /// <summary>The fields in the order the game's DTO would declare them, which is the order a save
        /// has to write them back in whatever order the file had.</summary>
        private static RecordSchema Record() => CatalogFixture.Record(
            IdField,
            CatalogFixture.Field(IdField, FieldKind.String),
            CatalogFixture.Field(NameField, FieldKind.String),
            CatalogFixture.Field(LevelField, FieldKind.Integer),
            CatalogFixture.Field(RarityField, FieldKind.String));

        private static ICatalogDescriptor Descriptor(string catalog) => CatalogFixture.Descriptor(
            catalog,
            RootShape.ArrayUnderKey,
            [CatalogFixture.Section(NpcsKey, Record())]);

        private CatalogWorkspace Load() =>
            CatalogWorkspace.Load(_root, [Descriptor(NpcCatalog), Descriptor(TablesCatalog)]);

        private CatalogView Npcs()
        {
            CatalogWorkspace workspace = CatalogWorkspace.Load(_root, [Descriptor(NpcCatalog)]);

            Assert.AreEqual(1, workspace.Catalogs.Count);

            return workspace.Catalogs[0];
        }

        private void Write(string catalog, string file, string content) =>
            CatalogFixture.Write(_root, catalog, file, content);

        /// <summary>Puts a folder where a file was, which is the one way to make a path refuse to be
        /// written that behaves the same on every machine the tests run on.</summary>
        private void Block(string catalog, string file)
        {
            string path = Path.Combine(_root, catalog, file);

            File.Delete(path);
            Directory.CreateDirectory(path);
        }

        private string Read(string file) => File.ReadAllText(Path.Combine(_root, NpcCatalog, file));
    }
}
