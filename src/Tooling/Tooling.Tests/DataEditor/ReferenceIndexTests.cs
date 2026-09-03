namespace Tooling.Tests.DataEditor
{
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// What a field pointing at another catalog may be answered with: the ids of the catalogs it names,
    /// whether a word standing in it answers to one of them, and which of them a query names.
    /// <para>The ids follow the run. An id is a field like any other and an author retypes them, so an
    /// index that answered from what was read at load would be answering for a catalog nobody has.</para>
    /// </summary>
    [TestClass]
    public class ReferenceIndexTests
    {
        private const string NpcCatalog = "Npc";
        private const string AbilityCatalog = "Abilities";
        private const string MissingCatalog = "Effects";

        /// <summary>A catalog whose records carry no id: they are listed by the place they stand in.</summary>
        private const string RowsCatalog = "LootRows";

        private const string NpcsKey = "npcs";
        private const string AbilitiesKey = "abilities";
        private const string RowsKey = "rows";
        private const string IdField = "id";
        private const string WeightField = "weight";

        private const string NpcFile = "Npc.json";
        private const string AbilityFile = "Abilities.json";
        private const string RowsFile = "LootRows.json";

        /// <summary>The name a record with no id of its own is listed under: its place in the section.</summary>
        private const string FirstPlaceName = "#0";

        private const string RonaldId = "Npc_Ronald";
        private const string SkeletonId = "Npc_Skeleton";
        private const string IceId = "Ability_Ice";
        private const string IceShardId = "Ability_Ice_Shard";
        private const string FireId = "Fire_Ability";
        private const string NewId = "Npc_Test";

        /// <summary>The address of the first npc's id, for the test that retypes it.</summary>
        private const string FirstNpcId = "/npcs/0/id";

        private const int EveryRow = 100;

        private const string TwoNpcs = """
            {
                "npcs": [
                    { "id": "Npc_Ronald" },
                    { "id": "Npc_Skeleton" }
                ]
            }
            """;

        private const string ThreeAbilities = """
            {
                "abilities": [
                    { "id": "Ability_Ice" },
                    { "id": "Fire_Ability" },
                    { "id": "Ability_Ice_Shard" }
                ]
            }
            """;

        private const string TwoRows = """
            {
                "rows": [
                    { "weight": 1 },
                    { "weight": 2 }
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

        [TestMethod]
        public void IdsOf_JoinsTheCatalogsTheFieldNames()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            CollectionAssert.AreEqual(
                new[] { RonaldId, SkeletonId, IceId, FireId, IceShardId },
                Rows(index.IdsOf([NpcCatalog, AbilityCatalog])));
        }

        [TestMethod]
        public void IdsOf_SaysNothingForACatalogNobodyDescribed()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            Assert.AreEqual(0, index.IdsOf([MissingCatalog]).Count);

            // Nothing, and the reason: a catalog this build cannot read is not a catalog with no records,
            // and a reference into one is not a reference that answers to nothing.
            CollectionAssert.AreEqual(new[] { MissingCatalog }, Rows(index.Undescribed([MissingCatalog])));
            Assert.AreEqual(0, index.Undescribed([NpcCatalog]).Count);
        }

        [TestMethod]
        public void IdsOf_PassesOverRecordsTheirCatalogGivesNoNameOf()
        {
            WriteBoth();
            CatalogFixture.Write(_root, RowsCatalog, RowsFile, TwoRows);

            var index = new ReferenceIndex(WithRows());

            // A record listed by its place in the section is not written under that name anywhere: the
            // catalog is described and read, and it simply has no id to answer a reference with.
            Assert.AreEqual(0, index.IdsOf([RowsCatalog]).Count);
            Assert.AreEqual(0, index.Undescribed([RowsCatalog]).Count);
            Assert.IsFalse(index.Exists([RowsCatalog], FirstPlaceName));
        }

        [TestMethod]
        public void Exists_AnswersForTheCatalogsNamedAndNoOthers()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            Assert.IsTrue(index.Exists([NpcCatalog, AbilityCatalog], IceId));
            Assert.IsFalse(index.Exists([NpcCatalog], IceId));
            Assert.IsFalse(index.Exists([NpcCatalog], string.Empty));

            // The game reads its ids to the letter, so an id spelled in another case is another id.
            Assert.IsFalse(index.Exists([AbilityCatalog], IceId.ToUpperInvariant()));
        }

        [TestMethod]
        public void Search_RanksTheWholeIdAboveItsStartAndItsStartAboveWhatIsInsideIt()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            CollectionAssert.AreEqual(
                new[] { IceId, IceShardId },
                Rows(index.Search([AbilityCatalog], "ability_ice", EveryRow)));

            CollectionAssert.AreEqual(
                new[] { IceId, IceShardId, FireId },
                Rows(index.Search([AbilityCatalog], "ability", EveryRow)));
        }

        [TestMethod]
        public void Search_AnswersAnEmptyQueryWithWhatThereIsToPick()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            Assert.AreEqual(5, index.Search([NpcCatalog, AbilityCatalog], string.Empty, EveryRow).Count);
            Assert.AreEqual(2, index.Search([NpcCatalog, AbilityCatalog], string.Empty, 2).Count);
            Assert.AreEqual(1, index.Search([AbilityCatalog], IceId, 1).Count);
        }

        [TestMethod]
        public void Exists_FollowsAnIdRetyped()
        {
            WriteBoth();

            CatalogWorkspace workspace = Both();
            var index = new ReferenceIndex(workspace);

            Assert.IsTrue(index.Exists([NpcCatalog], RonaldId));

            workspace.Catalogs[0].Files[0].Document.SetValue(JsonPointer.Parse(FirstNpcId), new JValue(NewId));

            Assert.IsFalse(index.Exists([NpcCatalog], RonaldId));
            Assert.IsTrue(index.Exists([NpcCatalog], NewId));
        }

        [TestMethod]
        public void Exists_FollowsARecordWrittenAndOneTakenBack()
        {
            WriteBoth();

            CatalogWorkspace workspace = Both();
            var index = new ReferenceIndex(workspace);
            CatalogView npcs = workspace.Catalogs[0];

            Assert.IsTrue(CatalogEditing.AddRecord(npcs, sectionKey: null, fileChoice: null, NewId).Done);
            Assert.IsTrue(index.Exists([NpcCatalog], NewId));

            npcs.Files[0].Document.History.Undo();

            Assert.IsFalse(index.Exists([NpcCatalog], NewId));
        }

        [TestMethod]
        public void Exists_FollowsAFileTheRunLaidDown()
        {
            WriteBoth();

            CatalogWorkspace workspace = Both();
            var index = new ReferenceIndex(workspace);
            CatalogView npcs = workspace.Catalogs[0];
            var made = new CatalogFile(Path.Combine(_root, NpcCatalog, "Npc_Extra.json"), JsonTreeDocument.Parse("{}"));

            npcs.AddFile(made);

            Assert.IsTrue(made.Document.Insert(JsonPointer.Root, NpcsKey, new JArray(new JObject(new JProperty(IdField, NewId)))));
            Assert.IsTrue(index.Exists([NpcCatalog], NewId));
        }

        /// <summary>What came back, as an array a collection assertion can hold against another.</summary>
        private static string[] Rows(IReadOnlyList<string> ids) => [.. ids];

        private void WriteBoth()
        {
            CatalogFixture.Write(_root, NpcCatalog, NpcFile, TwoNpcs);
            CatalogFixture.Write(_root, AbilityCatalog, AbilityFile, ThreeAbilities);
        }

        private CatalogWorkspace Both() => CatalogWorkspace.Load(_root, Described());

        /// <summary>The two named catalogs and one whose records carry no id of their own.</summary>
        private CatalogWorkspace WithRows() =>
            CatalogWorkspace.Load(_root,
            [
                .. Described(),
                CatalogFixture.Descriptor(RowsCatalog, RootShape.ArrayUnderKey,
                    [CatalogFixture.Section(RowsKey, CatalogFixture.Record(null, CatalogFixture.Field(WeightField, FieldKind.Integer)))])
            ]);

        private static IEnumerable<ICatalogDescriptor> Described() =>
        [
            CatalogFixture.Descriptor(NpcCatalog, RootShape.ArrayUnderKey,
                [CatalogFixture.Section(NpcsKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String)))]),
            CatalogFixture.Descriptor(AbilityCatalog, RootShape.ArrayUnderKey,
                [CatalogFixture.Section(AbilitiesKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String)))])
        ];
    }
}
