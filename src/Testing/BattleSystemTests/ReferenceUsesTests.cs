namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using LastBreath.Descriptors;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// Where the shipped data writes one id, asked of the real catalogs and the real narrative vocabulary.
    /// The tooling tests hold the walk to hand-written schemas; this holds it to the files the game loads,
    /// which is the only place the two halves are seen doing one job: what a catalog's own schema can see,
    /// and what only the narrative's conditions and actions can say.
    /// <para>A record renamed without one of those halves leaves the run pointing at a name nobody has —
    /// silently, because a reference into a catalog is a string until something reads it.</para>
    /// </summary>
    [TestClass]
    public class ReferenceUsesTests
    {
        /// <summary>An npc the shipped narrative is built around: he speaks, he hands out a quest and he
        /// takes it back.</summary>
        private const string VeteranId = "Npc_Bandit_Veteran";

        /// <summary>The quest he hands out, named in his own conversation from inside its conditions.</summary>
        private const string FieldOfBonesId = "Quest_Field_Of_Bones";

        /// <summary>The one npc the shipped loot tables give a table of his own.</summary>
        private const string RatKingId = "Npc_Boss_Rat_King";

        /// <summary>An npc no catalog points at and a quest puts into the world: his name is written in an
        /// action, which is free json to every schema there is.</summary>
        private const string WolfId = "Npc_Wolf";

        /// <summary>A word no file of the run writes.</summary>
        private const string UnwrittenId = "Npc_Nobody_Writes_This";

        /// <summary>What the two of them are renamed to: words nothing writes and no section lists, so the
        /// rename is refused by nothing and every place holding the old name is one the tool moved.</summary>
        private const string RenamedVeteranId = "Npc_Bandit_Veteran_Renamed";

        private const string RenamedQuestId = "Quest_Field_Of_Bones_Renamed";

        private const string VeteranDialogueFile = "Npc_Bandit_Veteran.json";
        private const string FieldOfBonesFile = "Quest_Field_Of_Bones.json";
        private const string FangFile = "Quest_Trial_Of_The_Fang.json";
        private const string LootTablesFile = "LootTables.json";

        /// <summary>The npc a conversation belongs to.</summary>
        private const string DialogueNpcAt = "/dialogues/0/npcId";

        /// <summary>The npc a quest is taken from, and the one it is handed back to.</summary>
        private const string GiverAt = "/quests/0/giverNpcId";

        private const string TurnInAt = "/quests/0/turnInNpcIds/0";

        /// <summary>The table read for one npc's own kills, keyed by that npc.</summary>
        private const string IndividualTableAt = "/individual/0/key";

        /// <summary>The npc a stage of a quest puts into the world — written inside an action, which no
        /// catalog schema reads.</summary>
        private const string SpawnedAt = "/quests/0/stages/0/onEnter/0/npcId";

        /// <summary>The quest a conversation opens on, and the one an option of it offers: both written
        /// inside conditions, where only the narrative's own finder can see them.</summary>
        private const string EntryQuestAt = "/dialogues/0/entryRules/0/conditions/0/questId";

        private const string OptionQuestAt = "/dialogues/0/nodes/0/options/0/visibleConditions/0/questId";

        private static CatalogWorkspace s_workspace = null!;

        /// <summary>The index a host builds: the catalogs' own schemas, and the finder that reads what they
        /// cannot.</summary>
        private static ReferenceUses s_uses = null!;

        /// <summary>The same index without that finder — what the run would answer if the narrative's own
        /// words were left to the schemas.</summary>
        private static ReferenceUses s_schemasOnly = null!;

        [ClassInitialize]
        public static void ReadTheShippedData(TestContext context)
        {
            s_workspace = CatalogWorkspace.Load(SharedData.Root(), CatalogDescriptors.All);
            s_uses = new ReferenceUses(s_workspace, [new NarrativeReferenceUses()]);
            s_schemasOnly = new ReferenceUses(s_workspace);
        }

        /// <summary>The run has to be reading something: an index over a workspace that opened no catalog
        /// would answer nothing to everything and pass forever.</summary>
        [TestMethod]
        public void TheRunOpensTheCatalogsAtAll()
        {
            Assert.AreNotEqual(0, s_workspace.Catalogs.Count);
            Assert.AreNotEqual(0, s_workspace.RecordCount);
        }

        /// <summary>What the catalogs' own schemas say about one npc: the conversation that belongs to him,
        /// the quest he hands out and the one he takes back. These are three catalogs and three shapes of
        /// field, and a rename reaching only the first of them leaves the other two pointing nowhere.</summary>
        [TestMethod]
        public void UsesOf_AnNpc_FindsTheDialogueAndTheQuestThatNameHim()
        {
            string[] places = Places(VeteranId, DataCatalog.Npc);

            CollectionAssert.Contains(places, VeteranDialogueFile + DialogueNpcAt);
            CollectionAssert.Contains(places, FieldOfBonesFile + GiverAt);
            CollectionAssert.Contains(places, FieldOfBonesFile + TurnInAt);
        }

        /// <summary>The loot table written for one npc is keyed by that npc's own id — a section of a
        /// catalog whose key means something different in each of its four sections, and the only one of
        /// them that is a reference at all.</summary>
        /// <remarks>Asked of the rat king because he is the one npc the shipped tables give a table of his
        /// own to.</remarks>
        [TestMethod]
        public void UsesOf_AnNpc_FindsTheLootTableWrittenForHim()
        {
            CollectionAssert.Contains(Places(RatKingId, DataCatalog.Npc), LootTablesFile + IndividualTableAt);
        }

        /// <summary>The ids written inside the narrative's conditions and actions, which the catalogs hold
        /// as free json: without the finder the host hands over, a rename would walk straight past a quest
        /// that puts an npc into the world and a conversation gated on a quest.</summary>
        [TestMethod]
        public void UsesOf_FindsTheIdsWrittenInsideConditionsAndActions()
        {
            CollectionAssert.Contains(Places(WolfId, DataCatalog.Npc), FangFile + SpawnedAt);

            Assert.AreNotEqual(0, s_uses.UsesOf(ReferenceTarget.Whole(DataCatalog.Quests), FieldOfBonesId).Count,
                "the conversation gates itself on the quest and nothing sees it");
        }

        /// <summary>The same places asked of an index built on the schemas alone. What the narrative writes
        /// in its own words is invisible there — which is what the finder exists for, and what a run without
        /// it would quietly leave behind.</summary>
        [TestMethod]
        public void WithoutTheNarrativeFinder_TheConditionsAndActionsAreNotSeen()
        {
            Assert.AreEqual(0, s_schemasOnly.UsesOf(ReferenceTarget.Whole(DataCatalog.Npc), WolfId).Count);
            Assert.AreEqual(0, s_schemasOnly.UsesOf(ReferenceTarget.Whole(DataCatalog.Quests), FieldOfBonesId).Count);

            // What the schemas do see is untouched by the finder being gone: the two halves add up, and
            // neither stands in for the other.
            CollectionAssert.Contains(
                Places(s_schemasOnly.UsesOf(ReferenceTarget.Whole(DataCatalog.Npc), VeteranId)),
                FieldOfBonesFile + GiverAt);
        }

        /// <summary>A word no file writes and a word nobody typed are both answered with nothing, over the
        /// whole of the shipped data.</summary>
        [TestMethod]
        public void UsesOf_AWordTheRunDoesNotWrite_IsAnsweredWithNothing()
        {
            Assert.AreEqual(0, s_uses.UsesOf(ReferenceTarget.Whole(DataCatalog.Npc), UnwrittenId).Count);
            Assert.AreEqual(0, s_uses.UsesOf(ReferenceTarget.Whole(DataCatalog.Npc), string.Empty).Count);
        }

        /// <summary>The rename as the tool carries it, over the data the game ships: the npc's new name is
        /// written into his own record and goes from there into the conversation that belongs to him and
        /// the quest he hands out — two catalogs and three shapes of field, none of which the panel
        /// showing the npc knows anything about.</summary>
        [TestMethod]
        public void RenameEverywhere_CarriesAnNpcIntoTheDialogueAndTheQuestThatNameHim()
        {
            Renaming run = ARename(DataCatalog.Npc, VeteranId);
            CatalogRenameResult result = Carried(run, RenamedVeteranId);

            Assert.IsTrue(result.Done, result.Refused);
            Assert.AreEqual(RenamedVeteranId, Word(run, DataCatalog.Dialogues, VeteranDialogueFile, DialogueNpcAt));
            Assert.AreEqual(RenamedVeteranId, Word(run, DataCatalog.Quests, FieldOfBonesFile, GiverAt));
            Assert.AreEqual(RenamedVeteranId, Word(run, DataCatalog.Quests, FieldOfBonesFile, TurnInAt));
            Assert.AreEqual(0, run.Uses.UsesOf(ReferenceTarget.Whole(DataCatalog.Npc), VeteranId).Count,
                "nothing of the run goes on naming him");

            AssertOneStepTakesItBack(run, VeteranId);
            Assert.AreEqual(VeteranId, Word(run, DataCatalog.Dialogues, VeteranDialogueFile, DialogueNpcAt));
            Assert.AreEqual(VeteranId, Word(run, DataCatalog.Quests, FieldOfBonesFile, GiverAt));
            Assert.AreEqual(VeteranId, Word(run, DataCatalog.Quests, FieldOfBonesFile, TurnInAt));
        }

        /// <summary>The half of the same gesture no schema can see: a quest is named inside the conditions
        /// of the conversation offering it, and a rename reaching only what the catalogs declare would
        /// leave that conversation gating itself on a quest nobody has.</summary>
        [TestMethod]
        public void RenameEverywhere_CarriesAQuestIntoTheConditionsThatGateOnIt()
        {
            Renaming run = ARename(DataCatalog.Quests, FieldOfBonesId);
            CatalogRenameResult result = Carried(run, RenamedQuestId);

            Assert.IsTrue(result.Done, result.Refused);
            Assert.AreEqual(RenamedQuestId, Word(run, DataCatalog.Dialogues, VeteranDialogueFile, EntryQuestAt));
            Assert.AreEqual(RenamedQuestId, Word(run, DataCatalog.Dialogues, VeteranDialogueFile, OptionQuestAt));
            Assert.AreEqual(0, run.Uses.UsesOf(ReferenceTarget.Whole(DataCatalog.Quests), FieldOfBonesId).Count,
                "nothing of the run goes on gating itself on the old name");

            AssertOneStepTakesItBack(run, FieldOfBonesId);
            Assert.AreEqual(FieldOfBonesId, Word(run, DataCatalog.Dialogues, VeteranDialogueFile, EntryQuestAt));
            Assert.AreEqual(FieldOfBonesId, Word(run, DataCatalog.Dialogues, VeteranDialogueFile, OptionQuestAt));
        }

        /// <summary>The id and everything naming it are one thing the author did: one press of undo, and
        /// the record answers to the word it was opened under.</summary>
        private static void AssertOneStepTakesItBack(Renaming run, string id)
        {
            Assert.AreEqual(1, run.History.Depth, "the rename is one step and not one per file");

            run.History.Undo();

            Assert.AreEqual(id, run.Record.CurrentId);
        }

        /// <summary>One run of the tool over the shipped data, with a record of it about to be renamed:
        /// every document on one stack, and the index a host builds — the catalogs' own schemas and the
        /// finder that reads what they cannot.</summary>
        private static Renaming ARename(string catalog, string id)
        {
            var history = new EditHistory();
            CatalogWorkspace workspace = CatalogWorkspace.Load(SharedData.Root(), CatalogDescriptors.All, history);
            CatalogView view = View(workspace, catalog);

            return new Renaming(
                workspace,
                new ReferenceUses(workspace, [new NarrativeReferenceUses()]),
                view,
                view.Records.First(record => record.CurrentId == id),
                history);
        }

        /// <summary>The gesture as a host makes it: the id is written into the record the way an author
        /// types it, and the rename is asked to carry everything else into the same step. In memory and
        /// nowhere else — nothing here saves, so the files the game reads are the files on disk.</summary>
        private static CatalogRenameResult Carried(Renaming run, string to)
        {
            CatalogRecord record = run.Record;
            string from = record.CurrentId;

            Assert.IsNotNull(record.Schema.IdField);
            Assert.IsTrue(record.File.Document.Put(record.Pointer.Append(record.Schema.IdField), new JValue(to)));

            return CatalogEditing.RenameEverywhere(run.Uses, run.View, record, from, to);
        }

        /// <summary>The word standing at an address of one named file of one catalog.</summary>
        private static string? Word(Renaming run, string catalog, string file, string at) =>
            View(run.Workspace, catalog).Files.First(one => one.Name == file)
                .Document.Resolve(JsonPointer.Parse(at))?.ToString();

        private static CatalogView View(CatalogWorkspace workspace, string catalog) =>
            workspace.Catalogs.First(one => one.Catalog == catalog);

        private static string[] Places(string id, string catalog) =>
            Places(s_uses.UsesOf(ReferenceTarget.Whole(catalog), id));

        /// <summary>What came back, one row per place, named the way an author would point at it.</summary>
        private static string[] Places(IReadOnlyList<ReferenceUse> uses) =>
            [.. uses.Select(use => $"{use.File.Name}{use.At}")];

        /// <summary>What one run of the tool has open while a record of the shipped data is renamed.</summary>
        private sealed record Renaming(
            CatalogWorkspace Workspace, ReferenceUses Uses, CatalogView View, CatalogRecord Record,
            EditHistory History);
    }
}
