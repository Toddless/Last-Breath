namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Seating an augment names two ids and holds neither, so the fitting rule is only worth as much
    /// as the thing that turns those ids into records. That thing is the ability data itself: the
    /// tags a fit is judged by are the tags the ability is built from, and the augment records are
    /// declared alongside them. These tests hold the seam end to end — the shipped data reaching the
    /// catalog through the real loader, the catalog reaching the board through the container each
    /// project composes, and the board refusing what the rule refuses instead of taking what it is
    /// handed.
    /// </summary>
    [TestClass]
    public class AbilityAugmentCatalogTests
    {
        private const string Slot = "socket_under_test";
        private const string ColdSlot = "socket_cold_ability";

        private const string UnwrittenAugment = "Augment_No_File_Declares";
        private const string UnwrittenAbility = "Ability_No_File_Declares";

        private const string PoisonAbility = "Ability_Test_Poison";
        private const string ColdAbility = "Ability_Test_Cold";
        private const string PoisonAugment = "Augment_Test_Poison_Tier_Two";

        /// <summary>The bootstraps that must end up with a board able to judge. The game project is
        /// not on the test assembly's references (it is the Godot game), so the fact that it composes
        /// the module carrying the registration is read off its source.</summary>
        private static readonly string[] s_bootstraps =
        [
            Path.Combine("Main", "Services", "GameServiceProvider.cs"),
            Path.Combine("Battle", "Services", "GameServiceProvider.cs"),
        ];

        /// <summary>The sources the game ships, found by walking up from the test binaries.</summary>
        private static string SrcRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                    directory = directory.Parent;
                Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
                return directory.FullName;
            }
        }

        [TestMethod]
        public void EveryAugmentTheShippedDataDeclaresIsFoundByItsOwnId()
        {
            // The records are declared inside the abilities and asked for by id alone: a drop, a
            // conversion or a window listing candidates knows the id it was offered and nothing else.
            IAbilityAugmentCatalog catalog = ShippedCatalog();
            int declared = 0;

            foreach (JObject ability in DeclaredAbilities())
                foreach (JObject augment in Declared(ability, "upgrades"))
                {
                    string id = augment.Value<string>("id") ?? string.Empty;
                    AbilityUpgradeData? record = catalog.Find(id);

                    Assert.IsNotNull(record, $"the catalog does not hold '{id}', declared by {ability.Value<string>("id")}");
                    Assert.AreEqual(augment.Value<int>("tier"), record.Tier, $"'{id}' came back carrying another tier");
                    declared++;
                }

            Assert.IsTrue(declared > 0, "the shipped data declares no augment at all, so the walk proves nothing");
        }

        [TestMethod]
        public void TheCatalogAnswersWithTheTagsTheAbilityDeclares()
        {
            IAbilityAugmentCatalog catalog = ShippedCatalog();
            int tags = 0;

            foreach (JObject ability in DeclaredAbilities())
            {
                string id = ability.Value<string>("id") ?? string.Empty;
                string[] declared = Tags(ability);

                CollectionAssert.AreEquivalent(declared, catalog.TagsOf(id).ToArray(),
                    $"the tags of '{id}' are not the ones its own data declares");
                tags += declared.Length;
            }

            Assert.IsTrue(tags > 0, "no shipped ability declares a tag, so an empty answer would pass the walk above");
        }

        [TestMethod]
        public void AnIdNoFileDeclaresIsNotFoundAndCarriesNoTags()
        {
            // Both halves are asked about ids arriving from a save file or a drop, so neither may
            // throw — but an augment nothing declares must not come back as a blank record either:
            // a record declaring nothing is still a record, and the rule would judge it.
            IAbilityAugmentCatalog catalog = ShippedCatalog();

            Assert.IsNull(catalog.Find(UnwrittenAugment));
            Assert.AreEqual(0, catalog.TagsOf(UnwrittenAbility).Count);
        }

        [TestMethod]
        public void TheCatalogIsTheProviderItselfAndNotASecondHolderOfTheSameData()
        {
            // Two holders of the ability records are two answers about the same augment, kept in step
            // only until one of them is loaded and the other is not.
            ServiceProvider container = BattleComposition();

            Assert.AreSame<object>(
                container.GetRequiredService<IAbilityProvider>(),
                container.GetRequiredService<IAbilityAugmentCatalog>());
        }

        [TestMethod]
        public void TheBoardTheCompositionBuildsJudgesTheAugmentsItIsHanded()
        {
            // The point of the wiring, on shipped ids: the board a project builds refuses a record the
            // rule refuses. The plain board is the control — the same id, the same slot, and it goes
            // in — so the refusal is the catalog reaching the board and not the slot being closed.
            ServiceProvider container = BattleComposition();
            Load(container.GetRequiredService<AbilityProvider>());
            var catalog = container.GetRequiredService<IAbilityAugmentCatalog>();
            (string ability, string augment, int tier) = AnAugmentAboveTierOne(catalog);
            IAbilitySocketBoard composed = container.GetRequiredService<IAbilitySocketBoard>();
            composed.Sync([new AbilitySocketPlacement(Slot, ability, tier - 1)]);

            Assert.IsFalse(composed.Install(Slot, augment), $"'{augment}' of tier {tier} went into a tier {tier - 1} slot");
            Assert.IsFalse(composed.Install(Slot, UnwrittenAugment), "an id nothing declares went in on the strength of its spelling");
            Assert.IsTrue(composed.Find(Slot)?.IsEmpty, "the refused augments are sitting in the slot anyway");

            var plain = new AbilitySocketBoard();
            plain.Sync([new AbilitySocketPlacement(Slot, ability, tier - 1)]);
            Assert.IsTrue(plain.Install(Slot, augment),
                "a board without a catalog stopped holding what it is handed, so the refusal above says nothing about the wiring");
        }

        [TestMethod]
        public void ARecordThatFitsIsSeatedAndTheSameRecordIsRefusedByAnotherAbility()
        {
            // Both verdicts on records that travelled the loading road the game uses, because the
            // fitting rule reads fields a hand-built record cannot prove are ever parsed. The content
            // is written by the test: no shipped record declares a tag or a binding yet, so the
            // shipped catalog can only produce refusals.
            IAbilityAugmentCatalog catalog = CatalogOver(TwoAbilitiesAndOneAugment());
            var board = new AbilitySocketBoard(catalog);
            board.Sync(
            [
                new AbilitySocketPlacement(Slot, PoisonAbility, 2),
                new AbilitySocketPlacement(ColdSlot, ColdAbility, 2),
            ]);

            Assert.IsTrue(board.Install(Slot, PoisonAugment), "the augment did not go onto the ability it shares a tag with");
            Assert.IsFalse(board.Install(ColdSlot, PoisonAugment), "the augment went onto an ability sharing none of its tags");
        }

        [TestMethod]
        public void EveryProjectBootstrapComposesTheModuleThatRegistersTheCatalog()
        {
            // The other half of the chain: the tests above resolve the catalog out of the module's own
            // composition, and this one says which projects compose that module. A project skipping it
            // builds the board that holds slots and judges nothing, and nothing else in the bootstrap
            // would show it — the catalog is resolved by type, so its absence is silent.
            foreach (string relative in s_bootstraps)
            {
                string bootstrap = Path.Combine(SrcRoot, relative);
                Assert.IsTrue(File.Exists(bootstrap), $"the bootstrap is not where it lived: {bootstrap}");
                Assert.IsTrue(File.ReadAllText(bootstrap).Contains("AddBattleSystemModuleDependencies", StringComparison.Ordinal),
                    $"{relative} composes the battle module no more, and its board is left without a catalog");
            }
        }

        /// <summary>The container a project builds around the battle module.</summary>
        private static ServiceProvider BattleComposition() =>
            new ServiceCollection().AddBattleSystemModuleDependencies().BuildServiceProvider();

        /// <summary>The shipped ability data as the game reads it: the real loader, the real parser.</summary>
        private static AbilityProvider ShippedCatalog()
        {
            var provider = new AbilityProvider();
            Load(provider);
            return provider;
        }

        private static void Load(AbilityProvider provider) => LoadFrom(SharedData.Root(), provider);

        /// <summary>The same loader over data the test wrote, for records the shipped file does not
        /// declare yet.</summary>
        private static IAbilityAugmentCatalog CatalogOver(string json)
        {
            string root = Directory.CreateTempSubdirectory("augments_").FullName;
            Directory.CreateDirectory(Path.Combine(root, DataCatalog.Abilities));
            File.WriteAllText(Path.Combine(root, DataCatalog.Abilities, "Abilities.json"), json);

            var provider = new AbilityProvider();
            LoadFrom(root, provider);
            Directory.Delete(root, recursive: true);
            return provider;
        }

        private static void LoadFrom(string root, AbilityProvider provider)
        {
            var service = new GameDataService(new FileSystemDataSource(root), [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
        }

        /// <summary>One poison ability carrying one poison augment, and a cold ability carrying none.</summary>
        private static string TwoAbilitiesAndOneAugment() =>
            $$"""
              {
                  "abilities": [
                      {
                          "id": "{{PoisonAbility}}",
                          "tags": [ "{{AbilityTags.Poison}}" ],
                          "upgrades": [
                              { "id": "{{PoisonAugment}}", "tier": 2, "tags": [ "{{AbilityTags.Poison}}" ] }
                          ]
                      },
                      { "id": "{{ColdAbility}}", "tags": [ "{{AbilityTags.Cold}}" ], "upgrades": [] }
                  ]
              }
              """;

        /// <summary>A shipped augment whose tier leaves a lower slot to be refused by, with the
        /// ability declaring it.</summary>
        private static (string Ability, string Augment, int Tier) AnAugmentAboveTierOne(IAbilityAugmentCatalog catalog)
        {
            foreach (JObject ability in DeclaredAbilities())
                foreach (JObject augment in Declared(ability, "upgrades"))
                {
                    string id = augment.Value<string>("id") ?? string.Empty;
                    if (catalog.Find(id) is not { Tier: > 1 } record) continue;

                    return (ability.Value<string>("id") ?? string.Empty, id, record.Tier);
                }

            Assert.Fail("the shipped data declares no augment above tier one, so no slot can be too small for one");
            return default;
        }

        /// <summary>Every ability record in the shipped catalog, read straight from the files so that
        /// the catalog is held against the data rather than against itself.</summary>
        private static IEnumerable<JObject> DeclaredAbilities()
        {
            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject ability in Declared(JObject.Parse(File.ReadAllText(path)), "abilities"))
                    yield return ability;
        }

        private static IEnumerable<JObject> Declared(JObject owner, string property) =>
            owner[property] is { } list ? list.OfType<JObject>() : [];

        private static string[] Tags(JObject ability) =>
            ability["tags"] is { } tags ? tags.Values<string>().OfType<string>().ToArray() : [];
    }
}
