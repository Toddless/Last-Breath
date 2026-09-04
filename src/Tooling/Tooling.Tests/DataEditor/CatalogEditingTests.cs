namespace Tooling.Tests.DataEditor
{
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Json;
    using Tooling.Localization;
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

        /// <summary>The record the file-per-slot catalog is written with.</summary>
        private const string RingId = "Ring_Signet";

        /// <summary>The two records of the sectioned catalog, one in each of its sections.</summary>
        private const string GeneralTableId = "Table_General";
        private const string IndividualTableId = "Table_Ronald";

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
        private const string HealthKey = "Health";
        private const string RowId = "Row_First";

        /// <summary>How a section with no word for itself would read if a refusal named it anyway: the
        /// quotes of the wording with nothing between them.</summary>
        private const string NamelessSection = "''";

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

        // ── renaming a record everywhere the run writes it ──────────────────────────────────────

        /// <summary>A second catalog, so that a rename has somewhere to reach that is not the record's
        /// own file.</summary>
        private const string QuestCatalog = "Quests";

        private const string QuestFile = "Quests.json";
        private const string QuestsKey = "quests";
        private const string AllyField = "ally";
        private const string NpcField = "npcId";
        private const string GuardsField = "guards";
        private const string DropsField = "drops";

        /// <summary>A map whose key and whose value are both the name being renamed: the value is
        /// addressed BY the key, so the two stand exactly as deep as each other and the key has to move
        /// last of the two.</summary>
        private const string OwnersField = "owners";

        /// <summary>A field standing INSIDE a map keyed by the name being renamed: it has to be written
        /// before the key moves, or its address goes with the key and the word is left standing.</summary>
        private const string ByField = "by";

        /// <summary>Where the .po files sit: among the catalogs, and read like one.</summary>
        private const string LocalizationFolder = "Localization";

        private const string DescriptionSuffix = "_Description";

        /// <summary>The address of the map a record is keyed into, for the tests that read it back.</summary>
        private const string DropsAt = "/quests/0/drops";

        private const string OwnersAt = "/quests/0/owners";

        /// <summary>A place no schema of the run reads: only a finder the host hands over knows the word
        /// is an id at all.</summary>
        private const string HiddenAt = "/quests/0/notes/hidden";

        /// <summary>The field of the OTHER npc pointing at the one being renamed: what makes the rename
        /// reach a second record of the record's own file.</summary>
        private const string AllyAt = "/npcs/1/ally";

        private const string QuestNpcAt = "/quests/0/npcId";

        /// <summary>The list writing the name twice. Both places are rewritten and both are counted: a
        /// walk that stopped at the first would leave a guard pointing at a record nobody has.</summary>
        private const string GuardsAt = "/quests/0/guards";

        /// <summary>A word no record answers, already keying the same map: a rename to it would have one
        /// key swallow the other.</summary>
        private const string FreeId = "Npc_Free";

        /// <summary>A word no record answers and every locale already writes: the records move and the
        /// wording cannot follow.</summary>
        private const string WordedId = "Npc_Worded";

        /// <summary>A word the record's own catalog never wrote: renaming from it moves nothing and
        /// refuses nothing — there is no place to carry.</summary>
        private const string UnwrittenId = "Npc_Nobody";

        /// <summary>What the host names the gesture, and so what one press of undo takes back.</summary>
        private const string RenameStep = "rename " + RonaldId + " → " + NewId;

        /// <summary>The suffixes the npc catalog words its records under: a name and a description.</summary>
        private static readonly string[] s_npcSuffixes = ["", DescriptionSuffix];

        private const string TwoAlliedNpcs = """
            {
                "npcs": [
                    { "id": "Npc_Ronald", "name": "Ronald", "ally": "Npc_Skeleton" },
                    { "id": "Npc_Skeleton", "name": "Skeleton", "ally": "Npc_Ronald" }
                ]
            }
            """;

        private const string OneQuest = """
            {
                "quests": [
                    {
                        "id": "Quest_One",
                        "npcId": "Npc_Ronald",
                        "guards": [ "Npc_Ronald", "Npc_Ronald" ],
                        "drops": {
                            "Npc_Ronald": { "by": "Npc_Ronald" },
                            "Npc_Free": { "by": "Npc_Skeleton" }
                        },
                        "owners": { "Npc_Ronald": "Npc_Ronald" },
                        "notes": { "hidden": "Npc_Ronald" }
                    }
                ]
            }
            """;

        private const string EnglishTexts = """
            msgid ""
            msgstr ""
            "Content-Type: text/plain; charset=UTF-8\n"
            "Language: en\n"

            msgid "Npc_Ronald"
            msgstr "Ronald"

            msgid "Npc_Ronald_Description"
            msgstr "A trader."

            msgid "Npc_Worded"
            msgstr "Somebody else."
            """;

        private const string RussianTexts = """
            msgid ""
            msgstr ""
            "Content-Type: text/plain; charset=UTF-8\n"
            "Language: ru\n"

            msgid "Npc_Ronald"
            msgstr "Роналд"

            msgid "Npc_Ronald_Description"
            msgstr "Торговец."

            msgid "Npc_Worded"
            msgstr "Кто-то ещё."
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

        /// <summary>A file the run lays down is written into before it joins the catalog, and the gesture
        /// that created it is a gesture like any other: it arrives on the run's own stack, so one press of
        /// undo takes it back and the file is left holding nothing to save.</summary>
        [TestMethod]
        public void AddRecord_PutsTheFileItLaidDownOnTheRunsHistory()
        {
            Write(EquipCatalog, RingFile, OneRing);

            EditHistory history = new();
            CatalogView view = CatalogWorkspace.Load(_root, [EquipmentDescriptor()], history).Catalogs[0];
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, BeltSlot, NewId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(2, view.Files.Count);

            CatalogFile laid = result.Record!.File;

            Assert.AreSame(history, laid.Document.History, "the file records where the rest of the run does");
            Assert.IsFalse(laid.Document.IsClean);
            Assert.IsNotNull(history.Undo());
            Assert.IsTrue(laid.Document.IsClean, "the file laid down holds nothing the author has not taken back");
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
            Assert.IsTrue(view.Files[0].Document.IsClean);
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
            Assert.IsTrue(view.Files[0].Document.IsClean);
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
        public void AddRecord_TakesANameAnotherSectionOfTheCatalogWrites()
        {
            Write(TablesCatalog, TablesFile, TwoSections);

            CatalogView view = Tables();
            CatalogEditResult result = CatalogEditing.AddRecord(view, IndividualKey, fileChoice: null, GeneralTableId);

            // The id is the key of the table its own section is read into: the same word under another
            // section is another record, and a catalog written that way on purpose can be edited.
            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(2, Section(view.Files[0], IndividualKey).Count);
            Assert.AreEqual(GeneralTableId, result.Record?.CurrentId);
        }

        [TestMethod]
        public void AddRecord_RefusesANameItsOwnSectionWrites()
        {
            Write(TablesCatalog, TablesFile, TwoSections);

            CatalogView view = Tables();
            CatalogEditResult result = CatalogEditing.AddRecord(view, IndividualKey, fileChoice: null, IndividualTableId);

            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Note, IndividualTableId);
            Assert.AreEqual(1, Section(view.Files[0], IndividualKey).Count);
            Assert.IsTrue(view.Files[0].Document.IsClean);
        }

        [TestMethod]
        public void AddRecord_RefusesANameTheSectionWritesInAnotherFile()
        {
            Write(EquipCatalog, RingFile, OneRing);

            CatalogView view = Equipment();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, BeltSlot, RingId);

            // One section spread over the folder's files is still one table to the game: the second
            // record of that name is the one that never loads, whichever file holds it.
            Assert.IsFalse(result.Done);
            Assert.AreEqual(1, view.Files.Count);
            Assert.IsTrue(view.Files[0].Document.IsClean);
        }

        [TestMethod]
        public void AddRecord_RefusesARecordNobodyNamed()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogEditResult result = CatalogEditing.AddRecord(view, sectionKey: null, fileChoice: null, "   ");

            Assert.IsFalse(result.Done);
            Assert.AreEqual(2, view.Records.Count);
            Assert.IsTrue(view.Files[0].Document.IsClean);
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
            Assert.IsTrue(view.Files[0].Document.IsClean);
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
            Assert.IsTrue(view.Files[0].Document.IsClean);
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
            Assert.IsTrue(untouched.Document.IsClean);

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
            Assert.IsTrue(view.Files[0].Document.IsClean);
        }

        [TestMethod]
        public void DuplicateRecord_TakesANameAnotherSectionWrites_AndRefusesOneItsOwnSectionDoes()
        {
            Write(TablesCatalog, TablesFile, TwoSections);

            CatalogView view = Tables();
            CatalogRecord general = view.Records[0];

            Assert.IsFalse(CatalogEditing.DuplicateRecord(view, general, GeneralTableId).Done, "its own section holds that name");

            CatalogEditResult result = CatalogEditing.DuplicateRecord(view, general, IndividualTableId);

            Assert.IsTrue(result.Done, result.Note);
            Assert.AreEqual(2, Section(view.Files[0], GeneralKey).Count);
            Assert.AreEqual(IndividualTableId, result.Record?.CurrentId);
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
            Assert.IsTrue(view.Files[0].Document.IsClean);
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

        /// <summary>The one judge of an id being spoken for — asked by every gesture that names a record,
        /// and the answer any path renaming one has to take.</summary>
        [TestMethod]
        public void Taken_AnswersForTheSectionAndNotForTheWholeCatalog()
        {
            Write(TablesCatalog, TablesFile, TwoSections);

            CatalogView view = Tables();

            Assert.IsTrue(CatalogEditing.Taken(view, IndividualKey, IndividualTableId));
            Assert.IsTrue(CatalogEditing.Taken(view, IndividualKey, "table_ronald"), "case is not part of the answer");
            Assert.IsFalse(CatalogEditing.Taken(view, GeneralKey, IndividualTableId), "another section's name is free");
            Assert.IsFalse(CatalogEditing.Taken(view, IndividualKey, GeneralTableId));
        }

        /// <summary>A catalog whose records stand at the root has one section and no word for it. The name
        /// is answered for all the same — the empty key IS that section — and a record asking for a name
        /// already written is refused without a section to name in the refusal.</summary>
        [TestMethod]
        public void Taken_AnswersForASectionWithNoKeyOfItsOwn()
        {
            Write(MapCatalog, MapFile, TwoUnits);
            Write(RowsCatalog, RowsFile, OneRow);

            CatalogView units = Units();
            CatalogView rows = Rows();

            Assert.IsTrue(CatalogEditing.Taken(units, RootKey, HealthKey));
            Assert.IsTrue(CatalogEditing.Taken(units, RootKey, "health"), "case is not part of the answer");
            Assert.IsFalse(CatalogEditing.Taken(units, RootKey, ManaKey));
            Assert.IsTrue(CatalogEditing.Taken(rows, RootKey, RowId));

            CatalogEditResult refused = CatalogEditing.AddRecord(units, sectionKey: null, fileChoice: null, HealthKey);

            Assert.IsFalse(refused.Done);
            StringAssert.Contains(refused.Note, HealthKey);

            // Named in the refusal is the id and nothing standing for a section: an author sent to look
            // in a section written as an empty word is sent nowhere.
            Assert.IsFalse(refused.Note!.Contains(NamelessSection, StringComparison.Ordinal), refused.Note);
            Assert.AreEqual(2, units.Records.Count);
        }

        /// <summary>A record renamed into a name its own section already writes: the name is in the
        /// record by the time the typing is over — the id is written as it is typed — and the refusal is
        /// worded like every other one, naming the record standing in the way and where to look for it.</summary>
        [TestMethod]
        public void RenameRefusal_RefusesANameItsOwnSectionAlreadyWrites()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogRecord record = view.Records[0];

            Assert.IsTrue(record.File.Document.Put(record.Pointer.Append(IdField), new JValue(SkeletonId)));

            string? refusal = CatalogEditing.RenameRefusal(view, record, SkeletonId);

            Assert.IsNotNull(refusal);
            StringAssert.Contains(refusal, SkeletonId);
            StringAssert.Contains(refusal, NpcsKey);
        }

        /// <summary>The same word under another section is another record, and a catalog written that way
        /// on purpose can be renamed within its own section as freely as it can be added to.</summary>
        [TestMethod]
        public void RenameRefusal_TakesANameAnotherSectionOfTheCatalogWrites()
        {
            Write(TablesCatalog, TablesFile, TwoSections);

            CatalogView view = Tables();

            Assert.IsNull(CatalogEditing.RenameRefusal(view, view.Records[0], IndividualTableId));
        }

        /// <summary>A record is never in its own way: the name is already written into it, and retyping it
        /// — or the same word in another case — has taken nothing from anybody. Asked the way the tool
        /// asks it: the catalog is read again on every keystroke, so the record the panel holds is not the
        /// instance the catalog now lists, and a record answered for by reference would be refused its own
        /// name.</summary>
        [TestMethod]
        public void RenameRefusal_TakesTheRecordsOwnNameWhateverItsCase()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();
            CatalogRecord record = view.Records[0];

            Assert.IsTrue(record.File.Document.Put(record.Pointer.Append(IdField), new JValue(RonaldId)));
            view.Reread();

            Assert.IsNull(CatalogEditing.RenameRefusal(view, record, RonaldId));
            Assert.IsNull(CatalogEditing.RenameRefusal(view, record, "npc_ronald"));
        }

        [TestMethod]
        public void RenameRefusal_TakesANameNobodyWrites()
        {
            Write(NpcCatalog, NpcFile, TwoNpcs);

            CatalogView view = Npcs();

            Assert.IsNull(CatalogEditing.RenameRefusal(view, view.Records[0], NewId));

            // A record left without a name is listed under the place it stands in, which nobody else can
            // be spoken for: an emptied box is not a rename to refuse.
            Assert.IsNull(CatalogEditing.RenameRefusal(view, view.Records[0], "   "));
        }

        /// <summary>The whole question a host asks before it lets a retyped id stand: the name the section
        /// may already write, and the word some map of the run may already be keyed by. A gate asking only
        /// the first lets the second through, and the author hears about it after the fact — from a rename
        /// that carried nothing while his box kept the word.</summary>
        [TestMethod]
        public void RenameRefusal_AskedOfTheRun_RefusesAWordAMapIsAlreadyKeyedBy()
        {
            Renaming run = ARun();
            CatalogRecord record = run.Ronald;

            string? blocked = CatalogEditing.RenameRefusal(run.Uses, run.Npcs, record, RonaldId, FreeId);

            Assert.IsNotNull(blocked);
            StringAssert.Contains(blocked, FreeId);
            StringAssert.Contains(blocked, QuestFile);

            // The section's own answer is asked first and said in its own words, and a word nothing writes
            // and nothing keys is no refusal at all.
            StringAssert.Contains(
                CatalogEditing.RenameRefusal(run.Uses, run.Npcs, record, RonaldId, SkeletonId), SkeletonId);

            Assert.IsNull(CatalogEditing.RenameRefusal(run.Uses, run.Npcs, record, RonaldId, NewId));
        }

        /// <summary>A word nobody typed either side is no rename, so there is nothing to refuse: the maps
        /// of the run are asked about a rename and not about a record standing still.</summary>
        [TestMethod]
        public void RenameRefusal_AskedOfTheRun_TakesAWordThatMovesNothing()
        {
            Renaming run = ARun();

            Assert.IsNull(CatalogEditing.RenameRefusal(run.Uses, run.Npcs, run.Ronald, RonaldId, RonaldId));
            Assert.IsNull(CatalogEditing.RenameRefusal(run.Uses, run.Npcs, run.Ronald, string.Empty, FreeId));
        }

        /// <summary>Every place the run wrote the old word is written again: a field of the record's own
        /// catalog, a field of another one, both elements of a list writing it twice, and the key a map
        /// stands under — which keeps the place it held among its neighbours.</summary>
        [TestMethod]
        public void RenameEverywhere_WritesTheNewNameEverywhereTheRunHeldTheOldOne()
        {
            Renaming run = ARun();
            CatalogRenameResult result = Rename(run, NewId);

            Assert.IsTrue(result.Done, result.Refused);
            Assert.AreEqual(8, result.Uses);
            Assert.AreEqual(2, result.Files, "the record's own catalog and the one pointing into it");

            Assert.AreEqual(NewId, Written(run.Npcs, AllyAt));
            Assert.AreEqual(NewId, Written(run.Quests, QuestNpcAt));

            CollectionAssert.AreEqual(new[] { NewId, NewId }, Words(run.Quests, GuardsAt));

            // The key is renamed and not written over, so the map is still read in the order the author
            // wrote it — and what stood under the old key is written before the key moves, or its address
            // would have gone with it.
            CollectionAssert.AreEqual(new[] { NewId, FreeId }, Keys(run.Quests, DropsAt));
            Assert.AreEqual(NewId, Under(run.Quests, DropsAt, NewId));
        }

        /// <summary>A map whose key and whose value are the same name: the value is addressed BY the key,
        /// so the two stand as deep as each other and only the order between them says whether both moved.
        /// The key goes last, or the value is left standing at an address nothing holds any more.</summary>
        [TestMethod]
        public void RenameEverywhere_WritesTheValueUnderAKeyBeforeTheKeyItself()
        {
            Renaming run = ARun();

            Assert.IsTrue(Rename(run, NewId).Done);

            CollectionAssert.AreEqual(new[] { NewId }, Keys(run.Quests, OwnersAt));
            Assert.AreEqual(NewId, Beside(run.Quests, OwnersAt, NewId));
        }

        /// <summary>The words only the host can see travel with the rest: the narrative writes ids inside
        /// its conditions, no schema of the run says they are ids, and a rename passing them over would
        /// leave a conversation gating itself on a record nobody has.</summary>
        [TestMethod]
        public void RenameEverywhere_CarriesThePlacesOnlyAFinderOfTheHostKnows()
        {
            Renaming run = ARun(workspace =>
                new Finder(workspace.Catalogs[1].Files[0], HiddenAt, RonaldId, ReferenceTarget.Whole(NpcCatalog)));

            CatalogRenameResult result = Rename(run, NewId);

            Assert.IsTrue(result.Done, result.Refused);
            Assert.AreEqual(9, result.Uses, "the eight places the schemas read, and the one only the host does");
            Assert.AreEqual(NewId, Written(run.Quests, HiddenAt));
        }

        /// <summary>The wording the catalog builds out of the id travels with it: every suffix, in every
        /// locale, and nothing else.</summary>
        [TestMethod]
        public void RenameEverywhere_CarriesTheWordingOfTheRecordWithIt()
        {
            Renaming run = ARun();
            CatalogRenameResult result = Rename(run, NewId);

            Assert.AreEqual(new LocalizedRename(2, null), result.Keys);
            Assert.AreEqual(RonaldName, run.Texts.Read(LocalizedTexts.ReferenceLocale, NewId));
            Assert.IsNotNull(run.Texts.Read(LocalizedTexts.AuthoringLocale, NewId + DescriptionSuffix));
            Assert.IsNull(run.Texts.Read(LocalizedTexts.ReferenceLocale, RonaldId));
            Assert.IsNull(run.Texts.Read(LocalizedTexts.AuthoringLocale, RonaldId + DescriptionSuffix));
        }

        /// <summary>How a record is worded is the HOST's answer and not always the catalog's: the same
        /// panel shows a record and the rows standing inside it, and a row of one words no key from the
        /// record's id. The suffixes it names are the ones that move, and no others.</summary>
        [TestMethod]
        public void RenameEverywhere_WordsTheKeysWithTheSuffixesItWasGiven()
        {
            Renaming run = ARun();
            CatalogRecord record = run.Ronald;

            Assert.IsTrue(record.File.Document.Put(record.Pointer.Append(IdField), new JValue(NewId)));

            CatalogRenameResult result = CatalogEditing.RenameEverywhere(
                run.Uses, run.Npcs, record, RonaldId, NewId, run.Texts, suffixes: [DescriptionSuffix]);

            Assert.IsTrue(result.Done, result.Refused);
            Assert.AreEqual(new LocalizedRename(1, null), result.Keys);
            Assert.IsNotNull(run.Texts.Read(LocalizedTexts.ReferenceLocale, NewId + DescriptionSuffix));

            // The catalog words its records under a name as well, and the name was not asked for: a key
            // the caller did not name is a key that belongs to something else on screen.
            Assert.AreEqual(RonaldName, run.Texts.Read(LocalizedTexts.ReferenceLocale, RonaldId));
            Assert.IsNull(run.Texts.Read(LocalizedTexts.ReferenceLocale, NewId));
        }

        /// <summary>The id, the references and the wording are one thing the author did. Taken back half
        /// way, a record is read under one word by its own file and under another by everything naming
        /// it — the state nothing on screen would show him.</summary>
        [TestMethod]
        public void RenameEverywhere_IsOneStepUndoTakesBackWhole()
        {
            Renaming run = ARun();

            Assert.IsTrue(Rename(run, NewId).Done);
            Assert.AreEqual(1, run.History.Depth, "the id typed and everything it carried are one step");
            Assert.AreEqual(RenameStep, run.History.NextUndo);

            run.History.Undo();

            Assert.AreEqual(RonaldId, run.Npcs.Records[0].CurrentId);
            Assert.AreEqual(RonaldId, Written(run.Npcs, AllyAt));
            Assert.AreEqual(RonaldId, Written(run.Quests, QuestNpcAt));
            CollectionAssert.AreEqual(new[] { RonaldId, RonaldId }, Words(run.Quests, GuardsAt));
            CollectionAssert.AreEqual(new[] { RonaldId, FreeId }, Keys(run.Quests, DropsAt));
            Assert.AreEqual(RonaldId, Under(run.Quests, DropsAt, RonaldId));
            Assert.IsNotNull(run.Texts.Read(LocalizedTexts.ReferenceLocale, RonaldId));
            Assert.IsFalse(run.Texts.IsDirty, "the locales are back where they were opened");
        }

        /// <summary>A name the section already writes is the one the game drops, so the rename is refused
        /// whole: nothing else in the run is touched, and the author is left with one word to fix rather
        /// than a run half rewritten.</summary>
        [TestMethod]
        public void RenameEverywhere_RefusesANameTheSectionAlreadyWrites_AndCarriesNothing()
        {
            Renaming run = ARun();
            CatalogRenameResult result = Rename(run, SkeletonId);

            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Refused, SkeletonId);
            Assert.AreEqual(0, result.Uses);
            AssertNothingWasCarried(run);
        }

        /// <summary>A word the same map is already keyed by would have one key swallow the other, and what
        /// stood under the one that went is a value no undo can name.</summary>
        [TestMethod]
        public void RenameEverywhere_RefusesAWordTheSameMapIsAlreadyKeyedBy()
        {
            Renaming run = ARun();
            CatalogRenameResult result = Rename(run, FreeId);

            Assert.IsFalse(result.Done);
            StringAssert.Contains(result.Refused, FreeId);
            StringAssert.Contains(result.Refused, QuestFile);
            AssertNothingWasCarried(run);
        }

        /// <summary>The wording refusing to follow is a sentence to read and not a rename to take back:
        /// the records have moved by then, and the two answers are told apart.</summary>
        [TestMethod]
        public void RenameEverywhere_MovesTheRecordsWhenTheWordingWillNotFollow()
        {
            Renaming run = ARun();
            CatalogRenameResult result = Rename(run, WordedId);

            Assert.IsTrue(result.Done, result.Refused);
            Assert.AreEqual(8, result.Uses);
            Assert.AreEqual(new LocalizedRename(0, WordedId), result.Keys);
            Assert.AreEqual(WordedId, Written(run.Quests, QuestNpcAt));
            Assert.IsNotNull(run.Texts.Read(LocalizedTexts.ReferenceLocale, RonaldId), "the old keys stand where they were");
        }

        /// <summary>A word nobody typed and a word the run never wrote are both nothing to carry: no
        /// refusal, and no file touched on the way to finding that out.</summary>
        [TestMethod]
        public void RenameEverywhere_MovesNothingForAWordNobodyTypedOrNobodyWrote()
        {
            Renaming run = ARun();
            CatalogRecord record = run.Ronald;

            foreach (string to in new[] { string.Empty, RonaldId })
            {
                CatalogRenameResult nothing = CatalogEditing.RenameEverywhere(
                    run.Uses, run.Npcs, record, RonaldId, to, run.Texts);

                Assert.IsTrue(nothing.Done, to);
                Assert.AreEqual(0, nothing.Uses, to);
            }

            CatalogRenameResult unwritten = CatalogEditing.RenameEverywhere(
                run.Uses, run.Npcs, record, UnwrittenId, NewId, run.Texts);

            Assert.IsTrue(unwritten.Done);
            Assert.AreEqual(0, unwritten.Uses);
            Assert.AreEqual(0, unwritten.Files);
            AssertNothingWasCarried(run);
            Assert.IsTrue(run.Npcs.Files[0].Document.IsClean);
        }

        /// <summary>Nothing left the record's own id behind: the references and the wording are where the
        /// run opened with them.</summary>
        private static void AssertNothingWasCarried(Renaming run)
        {
            Assert.AreEqual(RonaldId, Written(run.Npcs, AllyAt));
            Assert.AreEqual(RonaldId, Written(run.Quests, QuestNpcAt));
            CollectionAssert.AreEqual(new[] { RonaldId, FreeId }, Keys(run.Quests, DropsAt));
            Assert.AreEqual(RonaldId, Under(run.Quests, DropsAt, RonaldId));
            Assert.IsTrue(run.Quests.Files[0].Document.IsClean);
            Assert.IsFalse(run.Texts.IsDirty);
        }

        /// <summary>The word standing directly under one key of a map.</summary>
        private static string? Beside(CatalogView view, string at, string key) => Map(view, at)[key]?.ToString();

        /// <summary>The word standing at an address of a catalog's only file.</summary>
        private static string? Written(CatalogView view, string at) =>
            view.Files[0].Document.Resolve(JsonPointer.Parse(at))?.ToString();

        private static string[] Words(CatalogView view, string at) =>
            [.. ((JArray)view.Files[0].Document.Resolve(JsonPointer.Parse(at))!).Select(word => word.ToString())];

        private static JObject Map(CatalogView view, string at) =>
            (JObject)view.Files[0].Document.Resolve(JsonPointer.Parse(at))!;

        private static string[] Keys(CatalogView view, string at) =>
            [.. Map(view, at).Properties().Select(pair => pair.Name)];

        /// <summary>The word the entry under one key of a map points at.</summary>
        private static string? Under(CatalogView view, string at, string key) =>
            Map(view, at)[key]?[ByField]?.ToString();

        /// <summary>The gesture as a host makes it: the id is written into the record as the author types
        /// it, and the rename is asked to carry everything else into the same step.</summary>
        private static CatalogRenameResult Rename(Renaming run, string to)
        {
            CatalogRecord record = run.Ronald;

            Assert.IsTrue(record.File.Document.Put(record.Pointer.Append(IdField), new JValue(to)));

            return CatalogEditing.RenameEverywhere(run.Uses, run.Npcs, record, RonaldId, to, run.Texts);
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
        private CatalogView Equipment() => Catalog(EquipmentDescriptor());

        private static ICatalogDescriptor EquipmentDescriptor() =>
            CatalogFixture.Descriptor(EquipCatalog, RootShape.ArrayUnderKey,
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
                new FieldFilePlacement { FieldName = SlotField });

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

        /// <summary>A field pointing at any record of the npc catalog.</summary>
        private static FieldSchema PointsAtAnNpc(string jsonName) =>
            new() { JsonName = jsonName, Kind = FieldKind.Reference, RefTargets = [ReferenceTarget.Whole(NpcCatalog)] };

        /// <summary>The npcs, worded from their ids and pointing at one another: a rename has to reach a
        /// second record of the very file the id was typed in.</summary>
        private static ICatalogDescriptor NpcsThatAlly() =>
            CatalogFixture.Descriptor(NpcCatalog, RootShape.ArrayUnderKey,
                [
                    CatalogFixture.Section(NpcsKey, CatalogFixture.Record(IdField,
                        CatalogFixture.Required(IdField, FieldKind.String),
                        CatalogFixture.Field(NameField, FieldKind.String),
                        PointsAtAnNpc(AllyField)))
                ],
                placement: null,
                localizedSuffixes: s_npcSuffixes);

        /// <summary>A second catalog writing the npc's name in every way a word can be written: a field, an
        /// element of a list, and the key of a map.</summary>
        private static ICatalogDescriptor QuestsThatName() =>
            CatalogFixture.Descriptor(QuestCatalog, RootShape.ArrayUnderKey,
            [
                CatalogFixture.Section(QuestsKey, CatalogFixture.Record(IdField,
                    CatalogFixture.Required(IdField, FieldKind.String),
                    PointsAtAnNpc(NpcField),
                    new FieldSchema
                    {
                        JsonName = GuardsField,
                        Kind = FieldKind.Array,
                        Item = PointsAtAnNpc(FieldSchema.Unnamed)
                    },
                    new FieldSchema
                    {
                        JsonName = DropsField,
                        Kind = FieldKind.Dictionary,
                        Key = PointsAtAnNpc(FieldSchema.Unnamed),
                        Item = new FieldSchema
                        {
                            JsonName = FieldSchema.Unnamed,
                            Kind = FieldKind.Object,
                            Record = CatalogFixture.Record(null, PointsAtAnNpc(ByField))
                        }
                    },
                    new FieldSchema
                    {
                        JsonName = OwnersField,
                        Kind = FieldKind.Dictionary,
                        Key = PointsAtAnNpc(FieldSchema.Unnamed),
                        Item = PointsAtAnNpc(FieldSchema.Unnamed)
                    }))
            ]);

        /// <summary>The run a rename is made inside: two catalogs writing one npc's name, both locales
        /// wording it, and one stack under all of them — the way a host holds them.
        /// <para><paramref name="finder"/> is a source of the host's own, over the run once it is open:
        /// the narrative hands one over for the words its conditions write, and a rename has to reach
        /// those too.</para></summary>
        private Renaming ARun(Func<CatalogWorkspace, IReferenceUseSource>? finder = null)
        {
            Write(NpcCatalog, NpcFile, TwoAlliedNpcs);
            Write(QuestCatalog, QuestFile, OneQuest);
            Write(LocalizationFolder, LocalizedTexts.ReferenceLocale + PoCatalogSet.FileExtension, EnglishTexts);
            Write(LocalizationFolder, LocalizedTexts.AuthoringLocale + PoCatalogSet.FileExtension, RussianTexts);

            EditHistory history = new();
            CatalogWorkspace workspace = CatalogWorkspace.Load(_root, [NpcsThatAlly(), QuestsThatName()], history);

            return new Renaming(
                workspace,
                new ReferenceUses(workspace, finder is null ? null : [finder(workspace)]),
                LocalizedTexts.Load(Path.Combine(_root, LocalizationFolder), history),
                history);
        }

        /// <summary>A finder of the host's own: one word, at a place the schemas of the run read straight
        /// past. Anything else about it is the host's business — the point is that the rename reaches
        /// what the host found.</summary>
        private sealed class Finder(CatalogFile file, string at, string id, ReferenceTarget target) : IReferenceUseSource
        {
            public IEnumerable<ReferenceMention> Uses(CatalogWorkspace workspace) =>
                [new ReferenceMention(new ReferenceUse(file, JsonPointer.Parse(at), ReferenceUseKind.Value), id, [target])];
        }

        /// <summary>What one run of the tool has open while a record is renamed.</summary>
        private sealed record Renaming(
            CatalogWorkspace Workspace, ReferenceUses Uses, LocalizedTexts Texts, EditHistory History)
        {
            public CatalogView Npcs => Workspace.Catalogs[0];

            public CatalogView Quests => Workspace.Catalogs[1];

            /// <summary>The record being renamed, read again so that its address is the one the run holds
            /// now.</summary>
            public CatalogRecord Ronald => Npcs.Records[0];
        }
    }
}
