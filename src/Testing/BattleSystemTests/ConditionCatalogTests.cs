namespace LastBreathTest.BattleSystemTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Modifiers.Conditions;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The catalog side of conditions: a definition is written once under an id, and a consumer carries
    /// the id alone. What is proven here is what a catalog can silently get wrong — an id nobody
    /// defines, an id two entries claim, an entry that does not parse — plus the rule that makes one
    /// definition safe to share: every consumer gets its own instance, because a predicate holds the
    /// state of the fighter it watches.
    /// </summary>
    [TestClass]
    public class ConditionCatalogTests
    {
        private const float MaxHealth = 100f;
        private const string WhileLow = "Health_Below_Half";

        private static readonly string LowHealth = $$"""{ "id": "{{WhileLow}}", "type": "{{ConditionTypes.ResourceThreshold}}", "resource": "Health", "value": 0.5 }""";

        [TestMethod]
        public void TheShippedCatalogLoadsThroughTheDataPipelineAndEveryEntryResolves()
        {
            var provider = new ConditionProvider(ConditionParser.Default());
            var service = new GameDataService(new FileSystemDataSource(SharedData.Root()), [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add($"{context}: {exception.Message}");

            service.LoadAll();

            CollectionAssert.AreEqual(new[] { DataCatalog.Conditions }, provider.Catalogs.ToArray());
            Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
            Assert.IsTrue(ShippedEntries().Count > 0, "the shipped catalog is empty");

            foreach (var entry in ShippedEntries())
            {
                string id = entry.Value<string>(ConditionFields.Id) ?? string.Empty;
                Assert.IsTrue(provider.TryResolve(id, out var condition), $"the shipped entry '{id}' was refused");
                Assert.IsNotNull(condition, $"the shipped entry '{id}' produced no predicate");
            }
        }

        [TestMethod]
        public void TheShippedCatalogNamesEveryTypeThatHasAFactory()
        {
            var shipped = ShippedEntries()
                .Select(entry => entry.Value<string>(ConditionFields.Type) ?? string.Empty)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var factory in ConditionParser.BuiltInFactories())
                Assert.IsTrue(shipped.Contains(factory.Type),
                    $"no catalog entry is of type '{factory.Type}': a type nobody wrote down is first tried by whoever needs it, and its record format is proven by nothing");
        }

        [TestMethod]
        public void AnIdTheCatalogDoesNotHoldIsRefusedInsteadOfCountingAsAlwaysTrue()
        {
            var provider = Loaded(Catalog(LowHealth));

            bool resolved = provider.TryResolve("Health_Below_Nothing", out var condition);

            Assert.IsFalse(resolved, "an id nobody defines must drop its line, not let it apply unconditionally");
            Assert.IsNull(condition);
        }

        [TestMethod]
        public void ALineWithoutAnIdIsUnconditionalRatherThanBroken()
        {
            var provider = Loaded(Catalog(LowHealth));

            foreach (string? id in new[] { null, string.Empty, "  " })
            {
                Assert.IsTrue(provider.TryResolve(id, out var condition), "a line that names no condition is not a broken line");
                Assert.IsNull(condition);
            }
        }

        [TestMethod]
        public void AnIdClaimedTwiceKeepsTheDefinitionReadFirst()
        {
            string first = $$"""{ "id": "Doubled", "type": "{{ConditionTypes.Stance}}", "stance": "{{nameof(Stance.Strength)}}" }""";
            string second = $$"""{ "id": "Doubled", "type": "{{ConditionTypes.ResourceState}}", "resource": "Health", "state": "Full" }""";

            var withinOneFile = Loaded(Catalog(first, second));
            var acrossTwoFiles = Loaded(Catalog(first), Catalog(second));

            foreach (var provider in new[] { withinOneFile, acrossTwoFiles })
            {
                Assert.IsTrue(provider.TryResolve("Doubled", out var condition), "a doubled id must not cost the catalog its entry");
                Assert.IsInstanceOfType<StanceCondition>(condition,
                    "a doubled id has to settle on one definition, not on whichever entry happened to be read last");
            }
        }

        [TestMethod]
        public void ABrokenEntryCostsItselfAndTheRestOfTheCatalogStillLoads()
        {
            var provider = Loaded(Catalog(
                LowHealth,
                $$"""{ "type": "{{ConditionTypes.Stance}}", "stance": "{{nameof(Stance.Strength)}}" }""",
                """{ "id": "Unknown_Type", "type": "WhileTheMoonIsFull" }""",
                $$"""{ "id": "Broken_Share", "type": "{{ConditionTypes.ResourceThreshold}}", "resource": "Health", "value": 5 }""",
                $$"""{ "id": "Typoed_Resource", "type": "{{ConditionTypes.ResourceThreshold}}", "resource": "Stamina", "value": 0.5 }""",
                "\"an entry that is not even an object\"",
                $$"""{ "id": "Last_Entry", "type": "{{ConditionTypes.ResourceState}}", "resource": "Health", "state": "Full" }"""));

            Assert.IsTrue(provider.TryResolve(WhileLow, out _), "a broken entry took the entries before it down with it");
            Assert.IsTrue(provider.TryResolve("Last_Entry", out _), "a broken entry took the entries after it down with it");

            foreach (string id in new[] { "Unknown_Type", "Broken_Share", "Typoed_Resource" })
                Assert.IsFalse(provider.TryResolve(id, out _), $"'{id}' is broken data and became a live condition anyway");
        }

        [TestMethod]
        public void AFileThatIsNoCatalogFailsAloneWhileTheOtherFilesLoad()
        {
            var provider = new ConditionProvider(ConditionParser.Default());
            var source = new StubSource([
                new GameDataFile("broken.json", "{ \"nothing\": 1 }"),
                new GameDataFile("sound.json", Catalog(LowHealth)),
            ]);
            var service = new GameDataService(source, [provider]);
            List<string> failures = [];
            service.LoadFailed += (context, exception) => failures.Add(context);

            service.LoadAll();

            CollectionAssert.AreEqual(new[] { $"{DataCatalog.Conditions}/broken.json" }, failures,
                "the file-level policy of the data layer is one report per file, and only for the file that broke");
            Assert.IsTrue(provider.TryResolve(WhileLow, out _));
        }

        [TestMethod]
        public void ACatalogThatHoldsNothingYetRefusesIdsInsteadOfLettingLinesThrough()
        {
            // The state a project sits in between the container being built and LoadAll finishing, and
            // the state an emptied catalog file leaves behind. A consumer asking then must be refused:
            // "the catalog has not answered yet" is not "the line applies unconditionally".
            var unloaded = new ConditionProvider(ConditionParser.Default());
            var empty = Loaded(Catalog());

            foreach (var provider in new[] { unloaded, empty })
            {
                Assert.IsFalse(provider.TryResolve(WhileLow, out var condition),
                    "an id was answered by a catalog that holds nothing — the line it guards would apply to everyone");
                Assert.IsNull(condition);
                Assert.IsTrue(provider.TryResolve(null, out var unconditional),
                    "a line that names no condition needs no catalog behind it");
                Assert.IsNull(unconditional);
            }
        }

        [TestMethod]
        public void TheRegistrationLoadsAndServesOneAndTheSameCatalog()
        {
            // What a bootstrap gets for one AddConditionCatalog() call. Both halves can fail silently:
            // a second instance behind IGameDataParticipant would read the files nobody then queries,
            // and a factory set that missed the container would refuse every entry at load.
            var container = new ServiceCollection().AddConditionCatalog().BuildServiceProvider();

            var consumersFace = container.GetRequiredService<IConditionProvider>();
            var participants = container.GetServices<IGameDataParticipant>().ToList();

            Assert.AreEqual(1, participants.Count, "one catalog registration left the data layer with more than one participant to feed");
            Assert.AreSame<object>(consumersFace, participants[0],
                "the catalog files load into one instance while consumers resolve another");
            CollectionAssert.AreEquivalent(
                new[] { DataCatalog.Conditions }, participants[0].Catalogs.ToArray(),
                "the registered participant asks the data layer for a catalog other than Conditions");
            CollectionAssert.AreEquivalent(
                ConditionParser.BuiltInFactories().Select(factory => factory.Type).ToArray(),
                container.GetServices<IConditionFactory>().Select(factory => factory.Type).ToArray(),
                "a shipped predicate type never reached the container: every catalog entry of that type would be refused at load");
        }

        [TestMethod]
        public void TwoConsumersOfOneIdNeverShareState()
        {
            var provider = Loaded(Catalog(LowHealth));
            Assert.IsTrue(provider.TryResolve(WhileLow, out var mine));
            Assert.IsTrue(provider.TryResolve(WhileLow, out var yours));
            Assert.AreNotSame(mine, yours, "both consumers were handed the same predicate, and it can watch only one fighter");

            var wounded = Fighter(0.2f);
            var healthy = Fighter(1f);
            mine!.Attach(wounded);
            yours!.Attach(healthy);
            int myFlips = 0;
            mine.StateChanged += met => myFlips++;

            Assert.IsTrue(mine.IsMet, "the second consumer attaching took the first one's fighter away");
            Assert.IsFalse(yours.IsMet, "the second consumer answered from the first one's fighter");

            healthy.CurrentHealth = MaxHealth * 0.1f;

            Assert.IsTrue(yours.IsMet);
            Assert.IsTrue(mine.IsMet);
            Assert.AreEqual(0, myFlips, "one consumer's fighter pushed a flip through the other consumer's subscribers");
        }

        [TestMethod]
        public void AResolvedConditionArrivesUnattached()
        {
            var provider = Loaded(Catalog(LowHealth));
            Assert.IsTrue(provider.TryResolve(WhileLow, out var taken));
            taken!.Attach(Fighter(0.2f));

            Assert.IsTrue(provider.TryResolve(WhileLow, out var next));

            Assert.IsFalse(next!.IsMet, "the catalog handed out something that already watches a fighter");
        }

        [TestMethod]
        public void TheEntrysInversionFlagRidesThroughTheCatalog()
        {
            var provider = Loaded(Catalog(
                $$"""{ "id": "Clean", "type": "{{ConditionTypes.Status}}", "statuses": [ "{{nameof(StatusMasks.Debuff)}}" ], "negate": true }"""));
            Assert.IsTrue(provider.TryResolve("Clean", out var clean));
            var owner = Fighter(1f);

            clean!.Attach(owner);
            Assert.IsTrue(clean.IsMet);

            owner.ApplyStatus(StatusEffects.Poison);
            Assert.IsFalse(clean.IsMet);
        }

        private static ConditionProvider Loaded(params string[] files)
        {
            var provider = new ConditionProvider(ConditionParser.Default());
            for (int index = 0; index < files.Length; index++)
                provider.Apply(DataCatalog.Conditions, new GameDataFile($"catalog{index}.json", files[index]));

            return provider;
        }

        private static string Catalog(params string[] entries) =>
            $$"""{ "{{ConditionFields.Entries}}": [ {{string.Join(", ", entries)}} ] }""";

        private static ConditionOwner Fighter(float healthShare)
        {
            var owner = new ConditionOwner();
            owner.SetMaximum(EntityParameter.Health, MaxHealth);
            owner.CurrentHealth = MaxHealth * healthShare;
            return owner;
        }

        /// <summary>Every entry of the shipped catalog, read straight from the files instead of through
        /// the provider: the coverage checks have to see what the data says, including entries the
        /// provider would refuse.</summary>
        private static IReadOnlyList<JObject> ShippedEntries() =>
            Directory.EnumerateFiles(SharedData.Catalog(DataCatalog.Conditions), "*.json", SearchOption.AllDirectories)
                .SelectMany(path => JObject.Parse(File.ReadAllText(path))[ConditionFields.Entries]?.OfType<JObject>() ?? [])
                .ToList();

        /// <summary>A catalog handed to the load orchestrator file by file — the same shape the game
        /// sources have, without a folder on disk.</summary>
        private sealed class StubSource(IReadOnlyList<GameDataFile> files) : IGameDataSource
        {
            public IReadOnlyList<GameDataFile> ReadCatalog(string catalog) => files;
        }
    }
}
