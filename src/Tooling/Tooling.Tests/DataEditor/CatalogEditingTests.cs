namespace Tooling.Tests.DataEditor
{
    using System.IO;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// Writing, copying and taking out whole records: where a record lands in its section, which file it
    /// is written to, and what the catalog refuses to be asked for.
    /// <para>Every gesture is one step of the file's history — a record written and then taken back
    /// leaves the file exactly as it was, including the section it may have brought with it.</para>
    /// <para>The catalogs are hand-written like everywhere else in these tests: what is under test is
    /// driven by <see cref="RootShape"/> and by <see cref="FilePlacement"/>, and going through the
    /// reflector would be pinning the reflector a second time.</para>
    /// </summary>
    [TestClass]
    public class CatalogEditingTests
    {
        private const string NpcCatalog = "Npc";
        private const string EquipCatalog = "EquipItems";
        private const string TablesCatalog = "LootTables";
        private const string MapCatalog = "Formatting";
        private const string PoolsCatalog = "ModifierPools";
        private const string RulesCatalog = "CombatRules";

        /// <summary>A catalog whose file IS the array of records: it has no section key to write under,
        /// so a file laid down for a record of it can hold none.</summary>
        private const string RowsCatalog = "LootRows";

        private const string NpcsKey = "npcs";
        private const string ItemsKey = "items";
        private const string PoolsKey = "pools";
        private const string GeneralKey = "general";
        private const string IndividualKey = "individual";
        private const string RootKey = "";

        private const string IdField = "id";
        private const string KeyField = "key";
        private const string NameField = "name";
        private const string SlotField = "equipmentPart";
        private const string UnitField = "unit";
        private const string TurnsField = "turns";

        private const string RingSlot = "Ring";
        private const string BeltSlot = "Belt";

        private const string RonaldId = "Npc_Ronald";
        private const string SkeletonId = "Npc_Skeleton";
        private const string NewId = "Npc_Test";
        private const string CopyId = "Npc_Ronald_Copy";
        private const string RonaldName = "Ronald";

        private const string NpcFile = "Npc.json";
        private const string RingFile = "Ring.json";
        private const string BeltFile = "Belt.json";
        private const string TablesFile = "LootTables.json";
        private const string MapFile = "Formatting.json";
        private const string PoolsFile = "EquipItemModifierPools.json";
        private const string MythicPoolsName = "MythicModifiers";
        private const string MythicPoolsFile = MythicPoolsName + ".json";
        private const string RulesFile = "CombatRules.json";
        private const string RowsFile = "Rows.json";

        /// <summary>A file the catalog does not have, asked for by a name it may be given.</summary>
        private const string ExtraFileName = "Extra";

        /// <summary>A name no file can be given, whatever the catalog would like to call it.</summary>
        private const string UnusableName = "Mythic*Modifiers";

        /// <summary>A file the run cannot read, and so cannot be allowed to write over.</summary>
        private const string BrokenFile = "{ \"pools\": ";

        private const string ManaKey = "Mana";

        private const string TwoNpcs = """
            {
                "npcs": [
                    { "id": "Npc_Ronald", "name": "Ronald" },
                    { "id": "Npc_Skeleton", "name": "Skeleton" }
                ]
            }
            """;

        private const string OneNpc = """
            {
                "npcs": [
                    { "id": "Npc_Ronald", "name": "Ronald" }
                ]
            }
            """;

        private const string OneRing = """
            {
                "items": [
                    { "id": "Ring_Signet", "equipmentPart": "Ring" }
                ]
            }
            """;

        private const string TwoSections = """
            {
                "general": [
                    { "key": "Table_General" }
                ],
                "individual": [
                    { "key": "Table_Ronald" }
                ]
            }
            """;

        private const string OneSection = """
            {
                "general": [
                    { "key": "Table_General" }
                ]
            }
            """;

        private const string OneUnit = """
            {
                "Health": { "unit": "flat" }
            }
            """;

        private const string OnePool = """
            {
                "pools": [
                    { "id": "Pool_Weapon" }
                ]
            }
            """;

        private const string Rules = """
            {
                "turns": 3
            }
            """;

        private const string TwoUnits = """
            {
                "Health": { "unit": "flat" },
                "Armour": { "unit": "flat" }
            }
            """;

        private const string OneRow = """
            [
                { "id": "Row_First" }
            ]
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
        public void AddRecord_WritesTheRecordAtTheEndOfTheOnlySection()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, NewId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(3, view.Records.Count);
            Assert.AreEqual(NewId, result.Record?.CurrentId);

            // At the end of the section, and named there: an author reads his new record where he added
            // it, and the game reads it under the id he typed.
            Assert.AreEqual(NewId, view.Records[2].CurrentId);
            Assert.AreEqual(NewId, Section(view.Files[0], NpcsKey)[2][IdField]?.ToString());
        }

        [TestMethod]
        public void AddRecord_WritesIntoTheSectionItWasAskedFor()
        {
            Write(TablesCatalog, TablesFile, TwoSections);

            CatalogView view = Tables();
            CatalogEditResult result = CatalogEditing.AddRecord(view, IndividualKey, fileChoice: null, NewId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(2, Section(view.Files[0], IndividualKey).Count);
            Assert.AreEqual(1, Section(view.Files[0], GeneralKey).Count);
            StringAssert.StartsWith(result.Record?.Pointer.ToString(), "/" + IndividualKey);
        }

        [TestMethod]
        public void AddRecord_WritesTheSectionTheFileDoesNotCarryYet()
        {
            Write(TablesCatalog, TablesFile, OneSection);

            CatalogView view = Tables();
            CatalogEditResult result = CatalogEditing.AddRecord(view, IndividualKey, fileChoice: null, NewId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(1, Section(view.Files[0], IndividualKey).Count);

            // The section and the record it was written for arrive as one step: half a gesture taken back
            // is a file holding a section nobody asked for.
            Assert.IsNotNull(view.Files[0].Document.History.Undo());
            Assert.IsNull(view.Files[0].Document.Resolve(JsonPointer.Root.Append(IndividualKey)));
        }

        [TestMethod]
        public void AddRecord_WritesAKeyIntoTheMap()
        {
            Write(MapCatalog, MapFile, OneUnit);

            CatalogView view = Units();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, ManaKey);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(2, view.Records.Count);
            Assert.AreEqual(ManaKey, view.Records[1].CurrentId);
            Assert.IsNotNull(view.Files[0].Document.Resolve(JsonPointer.Root.Append(ManaKey)));
        }

        [TestMethod]
        public void AddRecord_FollowsTheFieldTheCatalogSplitsItsFilesBy()
        {
            Write(EquipCatalog, RingFile, OneRing);

            CatalogView view = Equipment();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, RingSlot, NewId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(RingFile, result.Record?.File.Name);

            // The choice is the record's own field: a record placed by a word it does not carry would be
            // found by the tool and never by the game.
            Assert.AreEqual(RingSlot, result.Record?.Token?[SlotField]?.ToString());
            Assert.AreEqual(1, view.Files.Count);
        }

        [TestMethod]
        public void AddRecord_LaysDownTheFileTheCatalogDoesNotHaveYet()
        {
            Write(EquipCatalog, RingFile, OneRing);

            CatalogView view = Equipment();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, BeltSlot, NewId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(2, view.Files.Count);
            Assert.AreEqual(BeltFile, result.Record?.File.Name);
            Assert.AreEqual(BeltSlot, result.Record?.Token?[SlotField]?.ToString());

            // The file is the run's from here on: it is dirty until it is written, and writing the
            // catalog puts it on disk beside the ones that were read.
            CatalogSaveResult saved = CatalogSaver.SaveDirty(view);

            Assert.AreEqual(0, saved.Notes.Count);
            Assert.IsTrue(File.Exists(Path.Combine(_root, EquipCatalog, BeltFile)));
        }

        [TestMethod]
        public void AddRecord_TakesTheFileFromTheCallerWhenNothingAboutTheRecordSaysIt()
        {
            Write(PoolsCatalog, PoolsFile, OnePool);
            Write(PoolsCatalog, MythicPoolsFile, OnePool.Replace("Pool_Weapon", "Pool_Mythic"));

            CatalogView view = Pools();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, MythicPoolsName, NewId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(MythicPoolsFile, result.Record?.File.Name);
            Assert.AreEqual(2, view.Files.Count);
        }

        [TestMethod]
        public void AddRecord_RefusesAFileNobodyNamed()
        {
            Write(PoolsCatalog, PoolsFile, OnePool);

            CatalogView view = Pools();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, NewId);

            Assert.IsFalse(result.Done);
            Assert.AreEqual(1, view.Records.Count);
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
        }

        [TestMethod]
        public void AddRecord_RefusesANameTheCatalogAlreadyWrites()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, SkeletonId);

            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Note, SkeletonId);

            // Refused and not half written: the file is where it was, with nothing to take back.
            Assert.AreEqual(2, view.Records.Count);
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
        }

        [TestMethod]
        public void AddRecord_RefusesANameThatOnlyDiffersInCase()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, "npc_skeleton");

            Assert.IsFalse(result.Done);
            Assert.AreEqual(2, view.Records.Count);
        }

        [TestMethod]
        public void AddRecord_RefusesARecordNobodyNamed()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, "   ");

            Assert.IsFalse(result.Done);
            Assert.AreEqual(2, view.Records.Count);
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
        }

        [TestMethod]
        public void AddRecord_RefusesASectionTheCatalogDoesNotWrite()
        {
            Write(TablesCatalog, TablesFile, TwoSections);

            CatalogView view = Tables();
            CatalogEditResult result = CatalogEditing.AddRecord(view, "fractions", fileChoice: null, NewId);

            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Note, "fractions");
        }

        [TestMethod]
        public void AddRecord_WritesTheFirstRecordOfACatalogWhoseFolderIsEmpty()
        {
            Directory.CreateDirectory(Path.Combine(_root, NpcCatalog));

            CatalogView view = Npcs();

            Assert.AreEqual(0, view.Files.Count);

            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, NewId);

            // The catalog knows its own folder, so a catalog with nothing in it yet is one an author may
            // write the first record of — the file arrives with it.
            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(1, view.Files.Count);
            Assert.AreEqual(NpcFile, result.Record?.File.Name);

            CatalogSaveResult saved = CatalogSaver.SaveDirty(view);

            Assert.AreEqual(0, saved.Notes.Count);
            Assert.IsTrue(File.Exists(Path.Combine(_root, NpcCatalog, NpcFile)));
        }

        [TestMethod]
        public void AddRecord_RefusesACatalogWithNoFolderOnDisk()
        {
            CatalogView view = Npcs();

            Assert.AreEqual(0, view.Files.Count);

            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, NewId);

            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Note, NpcCatalog);
        }

        [TestMethod]
        public void AddRecord_RefusesToWriteOverAFileThatIsOnDiskAndWasNotRead()
        {
            Write(PoolsCatalog, PoolsFile, OnePool);
            Write(PoolsCatalog, MythicPoolsFile, BrokenFile);

            CatalogView view = Pools();

            Assert.AreEqual(1, view.Files.Count);

            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, MythicPoolsName, NewId);

            // Named, refused, and the file left exactly as it is: a fresh document written over it would
            // drop everything the author cannot see.
            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Note, MythicPoolsFile);
            Assert.AreEqual(1, view.Files.Count);
            Assert.AreEqual(CatalogFixture.OnDisk(BrokenFile), File.ReadAllText(Path.Combine(_root, PoolsCatalog, MythicPoolsFile)));
        }

        [TestMethod]
        public void AddRecord_RefusesAFileNameNoFileCanBeGiven()
        {
            Write(PoolsCatalog, PoolsFile, OnePool);

            CatalogView view = Pools();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, UnusableName, NewId);

            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Note, UnusableName);
            Assert.AreEqual(1, view.Files.Count);
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
        }

        [TestMethod]
        public void AddRecord_LeavesNoFileBehindWhenTheRecordCouldNotBeWritten()
        {
            Write(RowsCatalog, RowsFile, OneRow);

            CatalogView view = Rows();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, ExtraFileName, NewId);

            // The catalog writes its records at the root of a file, and a file laid down for them arrives
            // holding an object: the record has nowhere to go, and a file with nothing in it is not one
            // the run keeps — it would be offered to the author and written on the next save.
            Assert.IsFalse(result.Done);
            Assert.AreEqual(1, view.Files.Count);
            Assert.AreEqual(1, view.Records.Count);
        }

        [TestMethod]
        public void AddRecord_RefusesACatalogThatIsOneRecord()
        {
            Write(RulesCatalog, RulesFile, Rules);

            CatalogView view = Settings();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, NewId);

            Assert.IsFalse(result.Done);
            Assert.AreEqual(1, view.Records.Count);
        }

        [TestMethod]
        public void AddRecord_IsOneStepThatUndoTakesBackWhole()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();

            Assert.IsTrue(CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, NewId).Done);
            Assert.IsNotNull(view.Files[0].Document.History.Undo());

            view.Reread();

            Assert.AreEqual(2, view.Records.Count);
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
        }

        [TestMethod]
        public void RemoveRecord_TakesTheRecordOutAndUndoPutsItBackWhereItStood()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.RemoveRecord(view, view.Records[0]);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(1, view.Records.Count);
            Assert.AreEqual(SkeletonId, view.Records[0].CurrentId);

            Assert.IsNotNull(view.Files[0].Document.History.Undo());

            view.Reread();

            Assert.AreEqual(2, view.Records.Count);
            Assert.AreEqual(RonaldId, view.Records[0].CurrentId);
        }

        [TestMethod]
        public void RemoveRecord_TakesOutTheLastRecordOfASection()
        {
            Write(NpcCatalog, NpcFile, OneNpc);

            CatalogView view = Npcs();

            Assert.IsTrue(CatalogEditing.RemoveRecord(view, view.Records[0]).Done);
            Assert.AreEqual(0, view.Records.Count);
            Assert.AreEqual(0, Section(view.Files[0], NpcsKey).Count);
        }

        [TestMethod]
        public void RemoveRecord_LeavesTheStepOnTheFileTheRecordWasTakenFrom()
        {
            Write(PoolsCatalog, PoolsFile, OnePool);
            Write(PoolsCatalog, MythicPoolsFile, OnePool.Replace("Pool_Weapon", "Pool_Mythic"));

            CatalogView view = Pools();
            CatalogFile emptied = view.Records[0].File;
            CatalogFile untouched = view.Records[1].File;

            Assert.IsTrue(CatalogEditing.RemoveRecord(view, view.Records[0]).Done);

            // The record left standing belongs to the other file, and that file has nothing to take back:
            // whoever offers undo has to reach the history the record was taken out of, and asking the
            // record now on screen would answer with a file the author never worked in.
            Assert.AreEqual(1, view.Records.Count);
            Assert.AreSame(untouched, view.Records[0].File);
            Assert.IsNotNull(emptied.Document.History.NextUndo);
            Assert.IsTrue(untouched.Document.History.IsClean);

            Assert.IsNotNull(emptied.Document.History.Undo());

            view.Reread();

            Assert.AreEqual(2, view.Records.Count);
        }

        [TestMethod]
        public void RemoveRecord_RefusesARecordTheDocumentNoLongerHolds()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogRecord record = view.Records[0];

            Assert.IsTrue(CatalogEditing.RemoveRecord(view, record).Done);

            CatalogEditResult again = CatalogEditing.RemoveRecord(view, view.Records[0]);
            CatalogEditResult gone = CatalogEditing.RemoveRecord(view, record);

            Assert.IsTrue(again.Done, again.Note);
            Assert.IsFalse(gone.Done);
        }

        [TestMethod]
        public void DuplicateRecord_WritesTheCopyRightAfterTheRecordItWasTakenFrom()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.DuplicateRecord(view, view.Records[0], CopyId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(3, view.Records.Count);
            Assert.AreEqual(CopyId, view.Records[1].CurrentId);

            // Everything but the name: an author copying a record edits what differs and not what he has
            // already written once.
            Assert.AreEqual(RonaldName, view.Records[1].Token?[NameField]?.ToString());
            Assert.AreEqual(RonaldId, view.Records[0].CurrentId);
        }

        [TestMethod]
        public void DuplicateRecord_RefusesANameTheCatalogAlreadyWrites()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.DuplicateRecord(view, view.Records[0], SkeletonId);

            Assert.IsFalse(result.Done);
            Assert.AreEqual(2, view.Records.Count);
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
        }

        [TestMethod]
        public void DuplicateRecord_RefusesACatalogThatIsOneRecordForBeingOne()
        {
            Write(RulesCatalog, RulesFile, Rules);

            CatalogView view = Settings();
            CatalogEditResult added = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, NewId);
            CatalogEditResult copied = CatalogEditing.DuplicateRecord(view, view.Records[0], NewId);

            // The same reason both gestures are refused for, and not "the record is no longer there":
            // the record is exactly where it was, and the catalog simply holds one of them.
            Assert.IsFalse(copied.Done);
            Assert.AreEqual(added.Note, copied.Note);
            Assert.AreEqual(1, view.Records.Count);
            Assert.IsTrue(view.Files[0].Document.History.IsClean);
        }

        [TestMethod]
        public void DuplicateRecord_PutsTheCopyOfAMapKeyAtTheEnd()
        {
            Write(MapCatalog, MapFile, TwoUnits);

            CatalogView view = Units();
            CatalogEditResult result = CatalogEditing.DuplicateRecord(view, view.Records[0], ManaKey);

            // A key is added where the object ends: in a map the copy is written last and not beside the
            // key it was taken from.
            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(3, view.Records.Count);
            Assert.AreEqual(ManaKey, view.Records[2].CurrentId);
        }

        [TestMethod]
        public void DuplicateRecord_CopiesAKeyOfTheMapUnderTheNameItWasGiven()
        {
            Write(MapCatalog, MapFile, OneUnit);

            CatalogView view = Units();
            CatalogEditResult result = CatalogEditing.DuplicateRecord(view, view.Records[0], ManaKey);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(2, view.Records.Count);
            Assert.AreEqual("flat", result.Record?.Token?[UnitField]?.ToString());
        }

        /// <summary>The array one section of a file holds.</summary>
        private static JArray Section(CatalogFile file, string key) =>
            (JArray)file.Document.Resolve(JsonPointer.Root.Append(key))!;

        private void Write(string catalog, string file, string content) =>
            CatalogFixture.Write(_root, catalog, file, content);

        private CatalogView Catalog(ICatalogDescriptor descriptor) =>
            CatalogWorkspace.Load(_root, [descriptor]).Catalogs[0];

        /// <summary>One array under one key, in one file — the plainest catalog there is.</summary>
        private CatalogView Npcs() =>
            Catalog(CatalogFixture.Descriptor(NpcCatalog, RootShape.ArrayUnderKey,
            [
                CatalogFixture.Section(NpcsKey, CatalogFixture.Record(IdField,
                    CatalogFixture.Required(IdField, FieldKind.String),
                    CatalogFixture.Field(NameField, FieldKind.String)))
            ]));

        /// <summary>A catalog with a file per value of one of its fields.</summary>
        private CatalogView Equipment() =>
            Catalog(CatalogFixture.Descriptor(EquipCatalog, RootShape.ArrayUnderKey,
                [
                    CatalogFixture.Section(ItemsKey, CatalogFixture.Record(IdField,
                        CatalogFixture.Required(IdField, FieldKind.String),
                        new FieldSchema
                        {
                            JsonName = SlotField,
                            Kind = FieldKind.Enum,
                            Required = true,
                            EnumValues = [RingSlot, BeltSlot]
                        }))
                ],
                new FieldFilePlacement { FieldName = SlotField }));

        /// <summary>A catalog nothing about a record places: the tool is told which file.</summary>
        private CatalogView Pools() =>
            Catalog(CatalogFixture.Descriptor(PoolsCatalog, RootShape.ArrayUnderKey,
                [CatalogFixture.Section(PoolsKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String)))],
                new FreeFilePlacement()));

        private CatalogView Tables() =>
            Catalog(CatalogFixture.Descriptor(TablesCatalog, RootShape.SectionsOfArrays,
            [
                CatalogFixture.Section(GeneralKey, CatalogFixture.Record(KeyField, CatalogFixture.Required(KeyField, FieldKind.String))),
                CatalogFixture.Section(IndividualKey, CatalogFixture.Record(KeyField, CatalogFixture.Required(KeyField, FieldKind.String)))
            ]));

        /// <summary>A map of id to record: the key IS the name, and no field carries it.</summary>
        private CatalogView Units() =>
            Catalog(CatalogFixture.Descriptor(MapCatalog, RootShape.Dictionary,
                [CatalogFixture.Section(RootKey, CatalogFixture.Record(null, CatalogFixture.Field(UnitField, FieldKind.String)))]));

        private CatalogView Settings() =>
            Catalog(CatalogFixture.Descriptor(RulesCatalog, RootShape.Single,
                [CatalogFixture.Section(RootKey, CatalogFixture.Record(null, CatalogFixture.Field(TurnsField, FieldKind.Integer)))]));

        /// <summary>A catalog whose files are the arrays themselves: the records stand at the root, under
        /// no key of their own.</summary>
        private CatalogView Rows() =>
            Catalog(CatalogFixture.Descriptor(RowsCatalog, RootShape.ArrayUnderKey,
                [CatalogFixture.Section(RootKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String)))],
                new FreeFilePlacement()));
    }
}
