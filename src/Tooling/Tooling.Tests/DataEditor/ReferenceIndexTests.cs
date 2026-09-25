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

        /// <summary>A catalog written in two sections that answer to nothing each other — what narrowing
        /// a reference to one section is for.</summary>
        private const string ResourceCatalog = "Resources";

        private const string CategoriesKey = "materialCategories";
        private const string MaterialsKey = "craftingResources";

        /// <summary>A section name the catalog does not hold: markup narrowed to it points at nothing.</summary>
        private const string MissingSection = "upgradeResources";

        private const string NpcsKey = "npcs";
        private const string AbilitiesKey = "abilities";
        private const string RowsKey = "rows";
        private const string IdField = "id";
        private const string WeightField = "weight";

        private const string NpcFile = "Npc.json";
        private const string AbilityFile = "Abilities.json";
        private const string RowsFile = "LootRows.json";
        private const string ResourceFile = "CraftingResources.json";

        /// <summary>The name a record with no id of its own is listed under: its place in the section.</summary>
        private const string FirstPlaceName = "#0";

        private const string RonaldId = "Npc_Ronald";
        private const string SkeletonId = "Npc_Skeleton";
        private const string IceId = "Ability_Ice";
        private const string IceShardId = "Ability_Ice_Shard";
        private const string FireId = "Fire_Ability";
        private const string NewId = "Npc_Test";

        private const string SteelId = "Category_Steel";
        private const string WoodId = "Category_Wood";
        private const string IngotId = "Resource_Steel_Ingot";

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

        private const string TwoSections = """
            {
                "materialCategories": [
                    { "id": "Category_Steel" },
                    { "id": "Category_Wood" }
                ],
                "craftingResources": [
                    { "id": "Resource_Steel_Ingot" }
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
                Rows(index.IdsOf([Whole(NpcCatalog), Whole(AbilityCatalog)])));
        }

        [TestMethod]
        public void IdsOf_SaysNothingForACatalogNobodyDescribed()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            Assert.AreEqual(0, index.IdsOf([Whole(MissingCatalog)]).Count);

            // Nothing, and the reason: a catalog this build cannot read is not a catalog with no records,
            // and a reference into one is not a reference that answers to nothing.
            CollectionAssert.AreEqual(new[] { MissingCatalog }, Rows(index.Undescribed([Whole(MissingCatalog)])));
            Assert.AreEqual(0, index.Undescribed([Whole(NpcCatalog)]).Count);
        }

        [TestMethod]
        public void IdsOf_PassesOverRecordsTheirCatalogGivesNoNameOf()
        {
            WriteBoth();
            CatalogFixture.Write(_root, RowsCatalog, RowsFile, TwoRows);

            var index = new ReferenceIndex(WithRows());

            // A record listed by its place in the section is not written under that name anywhere: the
            // catalog is described and read, and it simply has no id to answer a reference with.
            Assert.AreEqual(0, index.IdsOf([Whole(RowsCatalog)]).Count);
            Assert.AreEqual(0, index.Undescribed([Whole(RowsCatalog)]).Count);
            Assert.IsFalse(index.Exists([Whole(RowsCatalog)], FirstPlaceName));
        }

        [TestMethod]
        public void Exists_AnswersForTheCatalogsNamedAndNoOthers()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            Assert.IsTrue(index.Exists([Whole(NpcCatalog), Whole(AbilityCatalog)], IceId));
            Assert.IsFalse(index.Exists([Whole(NpcCatalog)], IceId));
            Assert.IsFalse(index.Exists([Whole(NpcCatalog)], string.Empty));

            // The game reads its ids to the letter, so an id spelled in another case is another id.
            Assert.IsFalse(index.Exists([Whole(AbilityCatalog)], IceId.ToUpperInvariant()));
        }

        /// <summary>A target naming a section is answered by that section alone, while the whole catalog
        /// still answers with everything in it: the two sections of a catalog like this one answer to
        /// nothing each other, and a field narrowed to one of them may hold nothing from the other.</summary>
        [TestMethod]
        public void IdsOf_UnderASection_AnswersWithThatSectionAndNotTheRestOfTheCatalog()
        {
            WriteSectioned();

            var index = new ReferenceIndex(WithSections());

            CollectionAssert.AreEqual(new[] { SteelId, WoodId }, Rows(index.IdsOf([In(ResourceCatalog, CategoriesKey)])));
            CollectionAssert.AreEqual(new[] { IngotId }, Rows(index.IdsOf([In(ResourceCatalog, MaterialsKey)])));
            CollectionAssert.AreEqual(new[] { SteelId, WoodId, IngotId }, Rows(index.IdsOf([Whole(ResourceCatalog)])));
        }

        /// <summary>An id of another section of the same catalog is not an answer. This is the whole of
        /// what the narrowing buys: the word is written in the file the field points at, and the game
        /// still resolves it against nothing.</summary>
        [TestMethod]
        public void Exists_UnderASection_RefusesAnIdOfAnotherSectionOfTheSameCatalog()
        {
            WriteSectioned();

            var index = new ReferenceIndex(WithSections());

            Assert.IsFalse(index.Exists([In(ResourceCatalog, CategoriesKey)], IngotId));
            Assert.IsTrue(index.Exists([In(ResourceCatalog, MaterialsKey)], IngotId));
            Assert.IsTrue(index.Exists([Whole(ResourceCatalog)], IngotId));
            Assert.IsTrue(index.Exists([In(ResourceCatalog, CategoriesKey)], SteelId));
        }

        /// <summary>The picker opens on what the field may actually hold: a query is ranked over the ids
        /// of the named section and no others.</summary>
        [TestMethod]
        public void Search_UnderASection_OffersThatSectionAlone()
        {
            WriteSectioned();

            var index = new ReferenceIndex(WithSections());

            CollectionAssert.AreEqual(
                new[] { SteelId, WoodId },
                Rows(index.Search([In(ResourceCatalog, CategoriesKey)], string.Empty, EveryRow)));
            Assert.AreEqual(0, index.Search([In(ResourceCatalog, CategoriesKey)], IngotId, EveryRow).Count);
            Assert.AreEqual(3, index.Search([Whole(ResourceCatalog)], string.Empty, EveryRow).Count);
        }

        /// <summary>A section the described catalog does not hold is named rather than answered with
        /// nothing: a narrowing nothing writes under would report every id in the field as broken, and
        /// that is the tool's own mistake to own up to.</summary>
        [TestMethod]
        public void Undescribed_NamesASectionTheCatalogDoesNotHold()
        {
            WriteSectioned();

            var index = new ReferenceIndex(WithSections());

            CollectionAssert.AreEqual(
                new[] { $"{ResourceCatalog}{ReferenceTarget.SectionSeparator}{MissingSection}" },
                Rows(index.Undescribed([In(ResourceCatalog, MissingSection)])));
            Assert.AreEqual(0, index.Undescribed([In(ResourceCatalog, CategoriesKey)]).Count);
            Assert.AreEqual(0, index.IdsOf([In(ResourceCatalog, MissingSection)]).Count);
        }

        [TestMethod]
        public void Search_RanksTheWholeIdAboveItsStartAndItsStartAboveWhatIsInsideIt()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            CollectionAssert.AreEqual(
                new[] { IceId, IceShardId },
                Rows(index.Search([Whole(AbilityCatalog)], "ability_ice", EveryRow)));

            CollectionAssert.AreEqual(
                new[] { IceId, IceShardId, FireId },
                Rows(index.Search([Whole(AbilityCatalog)], "ability", EveryRow)));
        }

        [TestMethod]
        public void Search_AnswersAnEmptyQueryWithWhatThereIsToPick()
        {
            WriteBoth();

            var index = new ReferenceIndex(Both());

            Assert.AreEqual(5, index.Search([Whole(NpcCatalog), Whole(AbilityCatalog)], string.Empty, EveryRow).Count);
            Assert.AreEqual(2, index.Search([Whole(NpcCatalog), Whole(AbilityCatalog)], string.Empty, 2).Count);
            Assert.AreEqual(1, index.Search([Whole(AbilityCatalog)], IceId, 1).Count);
        }

        [TestMethod]
        public void Exists_FollowsAnIdRetyped()
        {
            WriteBoth();

            CatalogWorkspace workspace = Both();
            var index = new ReferenceIndex(workspace);

            Assert.IsTrue(index.Exists([Whole(NpcCatalog)], RonaldId));

            workspace.Catalogs[0].Files[0].Document.SetValue(JsonPointer.Parse(FirstNpcId), new JValue(NewId));

            Assert.IsFalse(index.Exists([Whole(NpcCatalog)], RonaldId));
            Assert.IsTrue(index.Exists([Whole(NpcCatalog)], NewId));
        }

        [TestMethod]
        public void Exists_FollowsARecordWrittenAndOneTakenBack()
        {
            WriteBoth();

            CatalogWorkspace workspace = Both();
            var index = new ReferenceIndex(workspace);
            CatalogView npcs = workspace.Catalogs[0];

            Assert.IsTrue(CatalogEditing.AddRecord(npcs, sectionKey: null, fileChoice: null, NewId).Done);
            Assert.IsTrue(index.Exists([Whole(NpcCatalog)], NewId));

            npcs.Files[0].Document.History.Undo();

            Assert.IsFalse(index.Exists([Whole(NpcCatalog)], NewId));
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
            Assert.IsTrue(index.Exists([Whole(NpcCatalog)], NewId));
        }

        /// <summary>What came back, as an array a collection assertion can hold against another.</summary>
        private static string[] Rows(IReadOnlyList<string> ids) => [.. ids];

        /// <summary>The whole of a catalog: every section of it answers.</summary>
        private static ReferenceTarget Whole(string catalog) => ReferenceTarget.Whole(catalog);

        /// <summary>One section of a catalog, and nothing else in it.</summary>
        private static ReferenceTarget In(string catalog, string section) => new(catalog, section);

        private void WriteBoth()
        {
            CatalogFixture.Write(_root, NpcCatalog, NpcFile, TwoNpcs);
            CatalogFixture.Write(_root, AbilityCatalog, AbilityFile, ThreeAbilities);
        }

        private void WriteSectioned() => CatalogFixture.Write(_root, ResourceCatalog, ResourceFile, TwoSections);

        private CatalogWorkspace Both() => CatalogWorkspace.Load(_root, Described());

        /// <summary>The catalog written in two sections, on its own: what a reference narrowed to one of
        /// them is answered from.</summary>
        private CatalogWorkspace WithSections() =>
            CatalogWorkspace.Load(_root,
            [
                CatalogFixture.Descriptor(ResourceCatalog, RootShape.SectionsOfArrays,
                [
                    CatalogFixture.Section(CategoriesKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String))),
                    CatalogFixture.Section(MaterialsKey, CatalogFixture.Record(IdField, CatalogFixture.Required(IdField, FieldKind.String)))
                ])
            ]);

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
