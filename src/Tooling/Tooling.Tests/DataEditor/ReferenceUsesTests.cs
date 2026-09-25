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
    /// Where the run writes one id: the forward index turned round. What a rename has to carry with it,
    /// and what an author is about to break by retyping a word.
    /// <para>The three ways a word can stand are told apart here because a rename rewrites each of them
    /// differently — a value, an element of a list, the key a map is written under — and a walk that
    /// found only the first would leave a map keyed by a record nobody has.</para>
    /// </summary>
    [TestClass]
    public class ReferenceUsesTests
    {
        private const string NpcCatalog = "Npc";
        private const string ResourceCatalog = "Resources";
        private const string QuestCatalog = "Quests";

        /// <summary>A catalog whose records take shapes: which shape a record wears decides what its keys
        /// mean, and so whether a word in one of them is an id at all.</summary>
        private const string RuleCatalog = "Rules";

        private const string NpcsKey = "npcs";
        private const string QuestsKey = "quests";
        private const string RulesKey = "rules";
        private const string CategoriesKey = "materialCategories";
        private const string MaterialsKey = "craftingResources";

        private const string IdField = "id";
        private const string AllyField = "ally";
        private const string NpcField = "npcId";
        private const string GuardsField = "guards";
        private const string RewardField = "reward";
        private const string DropsField = "drops";
        private const string IntoField = "into";
        private const string TypeField = "type";
        private const string ResourceField = "resourceId";

        private const string KillType = "Kill";
        private const string GatherType = "Gather";

        private const string NpcFile = "Npc.json";
        private const string ResourceFile = "Resources.json";
        private const string QuestFile = "Quests.json";
        private const string RuleFile = "Rules.json";

        private const string RonaldId = "Npc_Ronald";
        private const string BanditId = "Npc_Bandit";

        /// <summary>An id no record of the run answers. A reference pointing at nothing is exactly the
        /// place its author has to be able to find, so it is a use like any other.</summary>
        private const string GhostId = "Npc_Ghost";

        /// <summary>One word written under one section of a catalog and pointed at from fields narrowed
        /// to the other: what the narrowing buys, asked from this end.</summary>
        private const string SharedId = "Shared_Word";

        private const string NewId = "Npc_Renamed";

        /// <summary>The address of the second quest's npc, for the test that retypes it.</summary>
        private const string SecondQuestNpc = "/quests/1/npcId";

        /// <summary>Where a source of the host's own says it found a word. Anything at all: nothing in the
        /// index reads it back, and the point is that it arrives.</summary>
        private const string SourcePlace = "/quests/0/entryRules/0/conditions/0/npcId";

        private const string TwoNpcs = """
            {
                "npcs": [
                    { "id": "Npc_Ronald", "ally": "Npc_Ronald" },
                    { "id": "Npc_Bandit", "ally": "" }
                ]
            }
            """;

        private const string TwoSections = """
            {
                "materialCategories": [
                    { "id": "Shared_Word" }
                ],
                "craftingResources": [
                    { "id": "Resource_Ingot" }
                ]
            }
            """;

        /// <summary>One quest writing a word in every way a word can be written, and a second one listed
        /// UNDER that word: a record's own name is not a reference, however much it looks like one.</summary>
        private const string TwoQuests = """
            {
                "quests": [
                    {
                        "id": "Quest_One",
                        "npcId": "Npc_Ronald",
                        "guards": [ "Npc_Ronald", "Npc_Bandit", "Npc_Ronald" ],
                        "reward": { "npcId": "Npc_Ronald" },
                        "drops": { "Npc_Ronald": 2, "Npc_Ghost": 1 },
                        "into": "Shared_Word"
                    },
                    {
                        "id": "Npc_Ronald",
                        "npcId": "Npc_Bandit",
                        "guards": [],
                        "reward": {},
                        "drops": {},
                        "into": ""
                    }
                ]
            }
            """;

        /// <summary>Three records of one catalog: one wearing a shape that reads its npc as an id, one
        /// wearing a shape that reads the same key as a word of its own, and one wearing a shape the
        /// schema does not hold at all.</summary>
        private const string ThreeRules = """
            {
                "rules": [
                    { "id": "Rule_Kill", "type": "Kill", "npcId": "Npc_Bandit" },
                    { "id": "Rule_Gather", "type": "Gather", "npcId": "Npc_Bandit", "resourceId": "Shared_Word" },
                    { "id": "Rule_Mystery", "type": "Mystery", "npcId": "Npc_Bandit", "resourceId": "Shared_Word" }
                ]
            }
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

        /// <summary>Every way a word stands, each at its own address and named for what it is. The record
        /// listed under the very same word is not among them: an id is a name and not a reference.</summary>
        [TestMethod]
        public void UsesOf_FindsAValue_AnElementOfAList_AndAKeyOfAMap()
        {
            WriteAll();

            var uses = new ReferenceUses(Loaded());

            CollectionAssert.AreEqual(
                new[]
                {
                    $"{NpcFile}/npcs/0/ally {ReferenceUseKind.Value}",
                    $"{QuestFile}/quests/0/npcId {ReferenceUseKind.Value}",
                    $"{QuestFile}/quests/0/guards/0 {ReferenceUseKind.ListItem}",
                    $"{QuestFile}/quests/0/guards/2 {ReferenceUseKind.ListItem}",
                    $"{QuestFile}/quests/0/reward/npcId {ReferenceUseKind.Value}",
                    $"{QuestFile}/quests/0/drops/Npc_Ronald {ReferenceUseKind.MapKey}",
                },
                Places(uses.UsesOf(Whole(NpcCatalog), RonaldId)));
        }

        /// <summary>A word written twice in one list is two places. They are rewritten one at a time and
        /// counted one at a time, and a walk that stopped at the first would leave the second behind.</summary>
        [TestMethod]
        public void UsesOf_ListsEveryPlaceOfOneList()
        {
            WriteAll();

            var uses = new ReferenceUses(Loaded());

            Assert.AreEqual(2, uses.UsesOf(Whole(NpcCatalog), RonaldId)
                .Count(use => use.Kind == ReferenceUseKind.ListItem));
        }

        /// <summary>A record naming itself is naming something, and the field holding its own name is a
        /// place the rename has to reach: the id moves and the field would go on pointing at the word the
        /// record used to be called.</summary>
        [TestMethod]
        public void UsesOf_FindsARecordNamingItself()
        {
            WriteAll();

            var uses = new ReferenceUses(Loaded());

            CollectionAssert.Contains(Places(uses.UsesOf(Whole(NpcCatalog), RonaldId)),
                $"{NpcFile}/npcs/0/ally {ReferenceUseKind.Value}");
        }

        /// <summary>A word nothing answers is written all the same. This is a use of a word and not a use
        /// of a record: an author looking for what points at a name he has just dropped is asking exactly
        /// this question.</summary>
        [TestMethod]
        public void UsesOf_FindsAWordNoRecordOfTheRunAnswers()
        {
            WriteAll();

            var uses = new ReferenceUses(Loaded());

            CollectionAssert.AreEqual(
                new[] { $"{QuestFile}/quests/0/drops/Npc_Ghost {ReferenceUseKind.MapKey}" },
                Places(uses.UsesOf(Whole(NpcCatalog), GhostId)));
        }

        /// <summary>A reference left empty names nothing on purpose. Answering it with every unfinished
        /// field of the run would be answering another question.</summary>
        [TestMethod]
        public void UsesOf_AnEmptyWord_IsWrittenNowhere()
        {
            WriteAll();

            var uses = new ReferenceUses(Loaded());

            Assert.AreEqual(0, uses.UsesOf(Whole(NpcCatalog), string.Empty).Count);
        }

        /// <summary>A field narrowed to one section of a catalog is not a use of a record standing in
        /// another, however the word is spelled: the game resolves it against that section alone.</summary>
        [TestMethod]
        public void UsesOf_UnderASection_PassesOverAFieldNarrowedToAnother()
        {
            WriteAll();

            var uses = new ReferenceUses(Loaded());

            // The quest's field points into the materials alone; the rules' points into the whole catalog
            // and so answers for a record of either section.
            CollectionAssert.AreEqual(
                new[]
                {
                    $"{RuleFile}/rules/1/resourceId {ReferenceUseKind.Value}",
                    $"{RuleFile}/rules/2/resourceId {ReferenceUseKind.Value}",
                },
                Places(uses.UsesOf(In(ResourceCatalog, CategoriesKey), SharedId)));

            Assert.AreEqual(3, uses.UsesOf(In(ResourceCatalog, MaterialsKey), SharedId).Count);
            Assert.AreEqual(3, uses.UsesOf(Whole(ResourceCatalog), SharedId).Count);
        }

        /// <summary>A record wearing a shape is read by that shape: the key one shape writes an id under
        /// is a word of its own in the other, and reading it by the wrong one would rewrite something the
        /// author never meant as a name.
        /// <para>A record wearing a shape the schema does not hold is read by all of them at once — the
        /// words are in the file whether or not it says which shape wrote them — except where the shapes
        /// disagree about a key, which nothing can vouch for.</para></summary>
        [TestMethod]
        public void UsesOf_ReadsARecordByTheShapeItWears_AndByEveryShapeWhenItWearsNone()
        {
            WriteAll();

            var uses = new ReferenceUses(Loaded());
            string[] npcs = Places(uses.UsesOf(Whole(NpcCatalog), BanditId));

            CollectionAssert.Contains(npcs, $"{RuleFile}/rules/0/npcId {ReferenceUseKind.Value}");

            CollectionAssert.DoesNotContain(npcs, $"{RuleFile}/rules/1/npcId {ReferenceUseKind.Value}",
                "the shape it wears writes a word of its own under that key");

            CollectionAssert.DoesNotContain(npcs, $"{RuleFile}/rules/2/npcId {ReferenceUseKind.Value}",
                "the shapes disagree about the key, so nothing vouches for what stands under it");

            // The key only one shape writes is read all the same: a record in no known shape still holds
            // the word, and passing it over would leave a rename half made.
            CollectionAssert.Contains(Places(uses.UsesOf(Whole(ResourceCatalog), SharedId)),
                $"{RuleFile}/rules/2/resourceId {ReferenceUseKind.Value}");
        }

        /// <summary>The uses follow the run. A word retyped is a word written somewhere else now, and an
        /// index built once at load would answer for a file nobody has.</summary>
        [TestMethod]
        public void UsesOf_FollowsAWordRetyped()
        {
            WriteAll();

            CatalogWorkspace workspace = Loaded();
            var uses = new ReferenceUses(workspace);

            Assert.AreEqual(3, uses.UsesOf(Whole(NpcCatalog), BanditId).Count);

            Quests(workspace).Files[0].Document.SetValue(JsonPointer.Parse(SecondQuestNpc), new JValue(GhostId));

            Assert.AreEqual(2, uses.UsesOf(Whole(NpcCatalog), BanditId).Count);
            Assert.AreEqual(2, uses.UsesOf(Whole(NpcCatalog), GhostId).Count);
        }

        /// <summary>A file the run laid down is watched like the ones it opened.</summary>
        [TestMethod]
        public void UsesOf_TakesInAFileTheRunLaidDown()
        {
            WriteAll();

            CatalogWorkspace workspace = Loaded();
            var uses = new ReferenceUses(workspace);
            CatalogView quests = Quests(workspace);
            var made = new CatalogFile(Path.Combine(_root, QuestCatalog, "Extra.json"), JsonTreeDocument.Parse("{}"));

            quests.AddFile(made);

            Assert.IsTrue(made.Document.Insert(JsonPointer.Root, QuestsKey,
                new JArray(new JObject(new JProperty(IdField, "Quest_Two"), new JProperty(NpcField, GhostId)))));

            Assert.AreEqual(2, uses.UsesOf(Whole(NpcCatalog), GhostId).Count);
        }

        /// <summary>A finder the host hands over is asked with the rest, and what it says is filed the
        /// same way: what no catalog schema can see is what such a finder is for.</summary>
        [TestMethod]
        public void UsesOf_JoinsWhatASourceOfTheHostFinds()
        {
            WriteAll();

            CatalogWorkspace workspace = Loaded();
            CatalogFile file = Quests(workspace).Files[0];
            var uses = new ReferenceUses(workspace, [new Finder(file, SourcePlace, RonaldId, Whole(NpcCatalog))]);

            CollectionAssert.Contains(Places(uses.UsesOf(Whole(NpcCatalog), RonaldId)),
                $"{QuestFile}{SourcePlace} {ReferenceUseKind.Value}");

            Assert.AreEqual(0, new ReferenceUses(workspace).UsesOf(Whole(NpcCatalog), RonaldId)
                .Count(use => use.At.ToString() == SourcePlace),
                "without the finder the place is one nothing in the run can see");
        }

        /// <summary>A place two finders both walk is one place. A source that repeats what the schemas
        /// already found must not have it rewritten twice and counted twice.</summary>
        [TestMethod]
        public void UsesOf_CountsAPlaceTwoFindersBothSeeOnce()
        {
            WriteAll();

            CatalogWorkspace workspace = Loaded();
            CatalogFile file = Quests(workspace).Files[0];
            var uses = new ReferenceUses(workspace, [new Finder(file, "/quests/0/npcId", RonaldId, Whole(NpcCatalog))]);

            Assert.AreEqual(6, uses.UsesOf(Whole(NpcCatalog), RonaldId).Count);
        }

        /// <summary>What a record answers to, as a reference names it: a record of a catalog written as
        /// one nameless section narrows nothing, because there is no section a field could have been
        /// pointed at.</summary>
        [TestMethod]
        public void Naming_NarrowsToTheSectionTheRecordStandsIn()
        {
            WriteAll();

            CatalogWorkspace workspace = Loaded();
            CatalogView resources = workspace.Catalogs.First(view => view.Catalog == ResourceCatalog);
            CatalogView npcs = workspace.Catalogs.First(view => view.Catalog == NpcCatalog);

            Assert.AreEqual(In(ResourceCatalog, CategoriesKey), ReferenceUses.Naming(resources, resources.Records[0]));
            Assert.AreEqual(In(NpcCatalog, NpcsKey), ReferenceUses.Naming(npcs, npcs.Records[0]));
        }

        /// <summary>What came back, one row per place, named the way an author would point at it.</summary>
        private static string[] Places(IReadOnlyList<ReferenceUse> uses) =>
            [.. uses.Select(use => $"{use.File.Name}{use.At} {use.Kind}")];

        private static ReferenceTarget Whole(string catalog) => ReferenceTarget.Whole(catalog);

        private static ReferenceTarget In(string catalog, string section) => new(catalog, section);

        private static FieldSchema Text(string jsonName) => CatalogFixture.Field(jsonName, FieldKind.String);

        private static FieldSchema Points(string jsonName, params ReferenceTarget[] targets) =>
            new() { JsonName = jsonName, Kind = FieldKind.Reference, RefTargets = targets };

        private static FieldSchema Element(params ReferenceTarget[] targets) =>
            Points(FieldSchema.Unnamed, targets);

        private static IEnumerable<ICatalogDescriptor> Described() =>
        [
            CatalogFixture.Descriptor(NpcCatalog, RootShape.ArrayUnderKey,
            [
                CatalogFixture.Section(NpcsKey, CatalogFixture.Record(IdField,
                    CatalogFixture.Required(IdField, FieldKind.String),
                    Points(AllyField, Whole(NpcCatalog))))
            ]),
            CatalogFixture.Descriptor(ResourceCatalog, RootShape.SectionsOfArrays,
            [
                CatalogFixture.Section(CategoriesKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String))),
                CatalogFixture.Section(MaterialsKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String)))
            ]),
            CatalogFixture.Descriptor(QuestCatalog, RootShape.ArrayUnderKey,
                [CatalogFixture.Section(QuestsKey, Quest())]),
            CatalogFixture.Descriptor(RuleCatalog, RootShape.ArrayUnderKey,
                [CatalogFixture.Section(RulesKey, Rule())])
        ];

        /// <summary>One record writing a word in every way a word can be written into a file.</summary>
        private static RecordSchema Quest() =>
            CatalogFixture.Record(IdField,
                CatalogFixture.Required(IdField, FieldKind.String),
                Points(NpcField, Whole(NpcCatalog)),
                new FieldSchema { JsonName = GuardsField, Kind = FieldKind.Array, Item = Element(Whole(NpcCatalog)) },
                new FieldSchema
                {
                    JsonName = RewardField,
                    Kind = FieldKind.Object,
                    Record = CatalogFixture.Record(null, Points(NpcField, Whole(NpcCatalog)))
                },
                new FieldSchema
                {
                    JsonName = DropsField,
                    Kind = FieldKind.Dictionary,
                    Key = Element(Whole(NpcCatalog)),
                    Item = CatalogFixture.Field(FieldSchema.Unnamed, FieldKind.Integer)
                },
                Points(IntoField, In(ResourceCatalog, MaterialsKey)));

        /// <summary>A record of two shapes that write one key over two different things.</summary>
        private static RecordSchema Rule() =>
            CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String)) with
            {
                Variants = new VariantSet(
                [
                    new VariantSchema
                    {
                        DiscriminatorValue = KillType,
                        Record = CatalogFixture.Record(null, Text(TypeField), Points(NpcField, Whole(NpcCatalog)))
                    },
                    new VariantSchema
                    {
                        DiscriminatorValue = GatherType,
                        Record = CatalogFixture.Record(null, Text(TypeField), Text(NpcField),
                            Points(ResourceField, Whole(ResourceCatalog)))
                    }
                ], TypeField)
            };

        private static CatalogView Quests(CatalogWorkspace workspace) =>
            workspace.Catalogs.First(view => view.Catalog == QuestCatalog);

        private CatalogWorkspace Loaded() => CatalogWorkspace.Load(_root, Described());

        private void WriteAll()
        {
            CatalogFixture.Write(_root, NpcCatalog, NpcFile, TwoNpcs);
            CatalogFixture.Write(_root, ResourceCatalog, ResourceFile, TwoSections);
            CatalogFixture.Write(_root, QuestCatalog, QuestFile, TwoQuests);
            CatalogFixture.Write(_root, RuleCatalog, RuleFile, ThreeRules);
        }

        /// <summary>A finder of the host's own, standing in for the one that reads the narrative's
        /// conditions: it says one place, and the index has to take it as its own.</summary>
        private sealed class Finder(CatalogFile file, string at, string id, ReferenceTarget target) : IReferenceUseSource
        {
            public IEnumerable<ReferenceMention> Uses(CatalogWorkspace workspace) =>
                [new ReferenceMention(new ReferenceUse(file, JsonPointer.Parse(at), ReferenceUseKind.Value), id, [target])];
        }
    }
}
