namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using static AugmentCopies;

    /// <summary>
    /// Seating an augment names two ids and holds neither, so the fitting rule is only worth as much
    /// as the thing that turns those ids into records. That thing is the augment catalog: a data
    /// participant of Core's own, reading the section that declares the records and the tags of the
    /// abilities a fit is judged against. These tests hold the seam end to end â€” the shipped data
    /// reaching the catalog through the real loader, the catalog reaching the board through the
    /// container a project composes, and the board refusing what the rule refuses instead of taking
    /// what it is handed.
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

        [TestMethod]
        public void EveryAugmentTheShippedDataDeclaresIsFoundByItsOwnId()
        {
            // The records are declared on their own and asked for by id alone: a drop, a conversion
            // or a window listing candidates knows the id it was offered and nothing else.
            IAbilityAugmentCatalog catalog = ShippedCatalog();
            int declared = 0;

            foreach (JObject augment in DeclaredAugments())
            {
                string id = augment.Value<string>("id") ?? string.Empty;
                AbilityUpgradeData? record = catalog.Find(id);

                Assert.IsNotNull(record, $"the catalog does not hold '{id}', which the section declares");
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
            // throw â€” but an augment nothing declares must not come back as a blank record either:
            // a record declaring nothing is still a record, and the rule would judge it.
            IAbilityAugmentCatalog catalog = ShippedCatalog();

            Assert.IsNull(catalog.Find(UnwrittenAugment));
            Assert.AreEqual(0, catalog.TagsOf(UnwrittenAbility).Count);
        }

        [TestMethod]
        public void TheCatalogIsOneSingletonAndNotASecondHolderOfTheSameSection()
        {
            // Two holders of the augment records are two answers about the same augment, kept in step
            // only until one of them is loaded and the other is not. The battle module used to
            // register the catalog itself and consumes one now, so a registration of its own would put
            // a fresh, unloaded instance in front of the one the loader feeds.
            IServiceCollection services = Composition();

            Assert.AreEqual(1, services.Count(descriptor => descriptor.ServiceType == typeof(IAbilityAugmentCatalog)),
                "the augment catalog is registered more than once, so what answers depends on which registration ran last");

            ServiceProvider container = services.BuildServiceProvider();
            Assert.AreSame<object>(
                container.GetRequiredService<AbilityAugmentCatalog>(),
                container.GetRequiredService<IAbilityAugmentCatalog>());
        }

        [TestMethod]
        public void TheBoardTheCompositionBuildsJudgesTheAugmentsItIsHanded()
        {
            // The point of the wiring, on shipped ids: the board a project builds refuses a record the
            // rule refuses. The plain board is the control â€” the same id, the same slot, and it goes
            // in â€” so the refusal is the catalog reaching the board and not the slot being closed.
            ServiceProvider container = ProjectComposition();
            LoadInto(container);
            var catalog = container.GetRequiredService<IAbilityAugmentCatalog>();
            (string ability, string augment, int tier) = AnAugmentAboveTierOne(catalog);
            IAbilitySocketBoard composed = container.GetRequiredService<IAbilitySocketBoard>();
            composed.Sync([new AbilitySocketPlacement(Slot, ability, tier - 1)]);

            Assert.IsFalse(composed.Install(Slot, Copy(augment)), $"'{augment}' of tier {tier} went into a tier {tier - 1} slot");
            Assert.IsFalse(composed.Install(Slot, Copy(UnwrittenAugment)), "an id nothing declares went in on the strength of its spelling");
            Assert.IsTrue(composed.Find(Slot)?.IsEmpty, "the refused augments are sitting in the slot anyway");

            var plain = new AbilitySocketBoard();
            plain.Sync([new AbilitySocketPlacement(Slot, ability, tier - 1)]);
            Assert.IsTrue(plain.Install(Slot, Copy(augment)),
                "a board without a catalog stopped holding what it is handed, so the refusal above says nothing about the wiring");
        }

        [TestMethod]
        public void ARecordThatFitsIsSeatedAndTheSameRecordIsRefusedByAnotherAbility()
        {
            // Both verdicts on records that travelled the loading road the game uses, because the
            // fitting rule reads fields a hand-built record cannot prove are ever parsed. The content
            // is written by the test rather than taken from the shipped catalog, so the pair under
            // test stays a pair whatever the shipped markup is repointed at.
            IAbilityAugmentCatalog catalog = ShippedAbilityData.CatalogOver(TwoAbilitiesAndOneAugment());
            var board = new AbilitySocketBoard(catalog);
            board.Sync(
            [
                new AbilitySocketPlacement(Slot, PoisonAbility, 2),
                new AbilitySocketPlacement(ColdSlot, ColdAbility, 2),
            ]);

            Assert.IsTrue(board.Install(Slot, Copy(PoisonAugment)), "the augment did not go onto the ability it shares a tag with");
            Assert.IsFalse(board.Install(ColdSlot, Copy(PoisonAugment)), "the augment went onto an ability sharing none of its tags");
        }

        /// <summary>What a project registers: the participants Core holds for every composition, and
        /// the battle module on top of them.</summary>
        private static IServiceCollection Composition() =>
            new ServiceCollection()
                .AddSharedGameDataParticipants()
                .AddBattleSystemModuleDependencies();

        private static ServiceProvider ProjectComposition() => Composition().BuildServiceProvider();

        /// <summary>The shipped data into a composed container, through the loader the game runs.</summary>
        private static void LoadInto(ServiceProvider container)
        {
            var service = new GameDataService(
                new FileSystemDataSource(SharedData.Root()),
                [container.GetRequiredService<AbilityAugmentCatalog>()]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
        }

        /// <summary>The shipped ability data as the game reads it: the real loader, the real parser.</summary>
        private static IAbilityAugmentCatalog ShippedCatalog() => ShippedAbilityData.Augments();

        /// <summary>One poison ability, one cold ability, and â€” beside them rather than inside either
        /// â€” one poison augment.</summary>
        private static string TwoAbilitiesAndOneAugment() =>
            $$"""
              {
                  "abilities": [
                      { "id": "{{PoisonAbility}}", "tags": [ "{{AbilityTags.Poison}}" ] },
                      { "id": "{{ColdAbility}}", "tags": [ "{{AbilityTags.Cold}}" ] }
                  ],
                  "augments": [
                      { "id": "{{PoisonAugment}}", "tier": 2, "tags": [ "{{AbilityTags.Poison}}" ] }
                  ]
              }
              """;

        /// <summary>A shipped augment whose tier leaves a lower slot to be refused by, together with
        /// an ability it belongs on â€” the record's own, so the refusal under test can only be the
        /// tier.</summary>
        private static (string Ability, string Augment, int Tier) AnAugmentAboveTierOne(IAbilityAugmentCatalog catalog)
        {
            foreach (JObject augment in DeclaredAugments())
            {
                string id = augment.Value<string>("id") ?? string.Empty;
                if (catalog.Find(id) is not { Tier: > 1 } record) continue;
                if (string.IsNullOrWhiteSpace(record.AbilityId)) continue;

                return (record.AbilityId, id, record.Tier);
            }

            Assert.Fail("the shipped data binds no augment above tier one, so no slot can be too small for one");
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

        /// <summary>Every augment record in the shipped catalog, read the same way.</summary>
        private static IEnumerable<JObject> DeclaredAugments()
        {
            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject augment in Declared(JObject.Parse(File.ReadAllText(path)), "augments"))
                    yield return augment;
        }

        private static IEnumerable<JObject> Declared(JObject owner, string property) =>
            owner[property] is { } list ? list.OfType<JObject>() : [];

        private static string[] Tags(JObject ability) =>
            ability["tags"] is { } tags ? tags.Values<string>().OfType<string>().ToArray() : [];
    }
}
