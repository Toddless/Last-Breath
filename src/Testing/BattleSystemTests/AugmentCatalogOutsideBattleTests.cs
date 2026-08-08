namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using static AugmentCopies;

    /// <summary>
    /// What an augment id means, asked where nobody fights. The records used to be read by the ability
    /// provider, which is the battle module's â€” so three of the five compositions the repository builds
    /// had no augments at all, and not because anyone decided they should not: they simply never
    /// composed the class that reads the section. Nothing said so, because an absent catalog is a
    /// board that judges nothing and a lookup that finds nothing.
    ///
    /// The reading is a data participant of Core's own now, registered where the participants common
    /// to every composition are. These walks hold that: a container with no battle module in it reads
    /// the whole section and judges seatings with it, and every project bootstrap goes through the one
    /// root that registers it.
    /// </summary>
    [TestClass]
    public class AugmentCatalogOutsideBattleTests
    {
        /// <summary>An ability no shipped record binds itself to â€” the control every refusal needs.
        /// Any id the book does not declare will do: the rule asks the record for a name and compares
        /// it, and a slot standing on an unwritten ability carries no tags to fall back on either.</summary>
        private const string Stranger = "Ability_No_File_Declares";

        /// <summary>The bootstraps that must end up holding the augment records. None of them is on
        /// the test assembly's references â€” they are Godot projects â€” so the fact that each goes
        /// through the shared root is read off its source.</summary>
        private static readonly string[] s_bootstraps =
        [
            Path.Combine("Main", "Services", "GameServiceProvider.cs"),
            Path.Combine("Battle", "Services", "GameServiceProvider.cs"),
            Path.Combine("LootGeneration", "Services", "GameServiceProvider.cs"),
            Path.Combine("Crafting", "Services", "GameServiceProvider.cs"),
        ];

        /// <summary>The shared composition root: the one thing all four bootstraps call.</summary>
        private static readonly string s_sharedRoot = Path.Combine("Core", "Services", "GameServiceProvider.cs");

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
        public void ACompositionWithoutTheBattleModuleReadsEveryAugmentTheSectionDeclares()
        {
            ServiceProvider container = CompositionWithoutBattle();
            Assert.IsNull(container.GetService<IAbilityProvider>(),
                "the composition under test builds the battle module after all, so it proves nothing about the ones that do not");

            LoadShippedData(container);

            var catalog = container.GetService<IAbilityAugmentCatalog>();
            Assert.IsNotNull(catalog, "a composition without the battle module holds no augment records at all");

            List<string> declared = DeclaredAugmentIds();
            Assert.IsTrue(declared.Count > 0, "the shipped section declares no augment, so the walk proves nothing");
            foreach (string id in declared)
                Assert.IsNotNull(catalog.Find(id), $"'{id}' is declared by the section and unknown outside the battle module");

            Assert.AreEqual(declared.Count, catalog.All.Count, "the catalog holds records the section does not write, or fewer than it does");
        }

        [TestMethod]
        public void TheSameCompositionJudgesASeatingWithNoBattleModuleInIt()
        {
            // Holding the records is half of it: a drop, a conversion or a window asks whether an
            // augment belongs somewhere, and that question needs the ability's tags as well. Both
            // answers come out of the same catalog, so both are asked here â€” on a bound record,
            // seated on the ability its own data names and refused by every other.
            ServiceProvider container = CompositionWithoutBattle();
            LoadShippedData(container);
            var catalog = container.GetRequiredService<IAbilityAugmentCatalog>();
            (string ability, string augment, int tier) = ABoundRecord(catalog);

            var board = new AbilitySocketBoard(catalog);
            board.Sync(
            [
                new AbilitySocketPlacement(Socket(ability), ability, tier),
                new AbilitySocketPlacement(Socket(Stranger), Stranger, tier),
            ]);

            Assert.IsTrue(board.Install(board.At(Socket(ability)), Copy(augment)),
                $"'{augment}' stayed out of a slot of '{ability}', the ability its own record names");
            Assert.IsFalse(board.Install(board.At(Socket(Stranger)), Copy(augment)),
                $"'{augment}' went onto '{Stranger}', which its record does not name");
        }

        [TestMethod]
        public void EveryProjectBootstrapGoesThroughTheRootThatRegistersTheSharedParticipants()
        {
            // The other half of the chain: the walks above resolve the catalog out of the shared
            // registration, and this one says who composes it. A bootstrap skipping the shared root
            // would lose the records without a word â€” they are resolved by type, so their absence is
            // silent, which is exactly how three compositions went without them before.
            string root = Path.Combine(SrcRoot, s_sharedRoot);
            Assert.IsTrue(File.Exists(root), $"the shared composition root is not where it lived: {root}");
            Assert.IsTrue(File.ReadAllText(root).Contains(nameof(GameDataDependencies.AddSharedGameDataParticipants), StringComparison.Ordinal),
                "the shared root registers the common data participants no more, and every project loses them at once");

            foreach (string relative in s_bootstraps)
            {
                string bootstrap = Path.Combine(SrcRoot, relative);
                Assert.IsTrue(File.Exists(bootstrap), $"the bootstrap is not where it lived: {bootstrap}");
                Assert.IsTrue(File.ReadAllText(bootstrap).Contains("GameServiceProvider.Initialize", StringComparison.Ordinal),
                    $"{relative} composes itself outside the shared root, and its augment records are gone with it");
            }
        }

        /// <summary>The container a project without the battle module builds â€” Core's own registrations
        /// and nothing else, which is what the loot and crafting sandboxes hold of the ability data.</summary>
        private static ServiceProvider CompositionWithoutBattle() =>
            new ServiceCollection().AddSharedGameDataParticipants().BuildServiceProvider();

        /// <summary>The shipped data into a composed container, through the loader the game runs.</summary>
        private static void LoadShippedData(ServiceProvider container)
        {
            var service = new GameDataService(
                new FileSystemDataSource(SharedData.Root()),
                container.GetServices<IGameDataParticipant>());
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
        }

        /// <summary>A shipped augment written for one named ability, together with that ability and the
        /// tier the record stands at.</summary>
        private static (string Ability, string Augment, int Tier) ABoundRecord(IAbilityAugmentCatalog catalog)
        {
            foreach (AbilityUpgradeData record in catalog.All)
                if (!string.IsNullOrWhiteSpace(record.AbilityId))
                    return (record.AbilityId, record.Id, record.Tier);

            Assert.Fail("the shipped data binds no augment to an ability, so a seating proves nothing about the binding");
            return default;
        }

        private static string Socket(string abilityId) => $"socket_{abilityId}";

        /// <summary>Every augment id the shipped section writes, read off the files themselves â€” the
        /// catalog is held against the data rather than against itself.</summary>
        private static List<string> DeclaredAugmentIds()
        {
            List<string> ids = [];

            foreach (string path in Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Abilities), "*.json", SearchOption.AllDirectories))
                foreach (JObject augment in JObject.Parse(File.ReadAllText(path))["augments"] as JArray ?? [])
                    ids.Add(augment.Value<string>("id") ?? string.Empty);

            return ids;
        }
    }
}
