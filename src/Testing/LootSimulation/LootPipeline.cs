namespace LastBreathTest.LootSimulation
{
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Entity.Components;
    using Core.Services;
    using LootGeneration.Internal;
    using LootGeneration.Services;
    using LootGeneration.Source;

    /// <summary>The REAL drop pipeline assembled without Godot: FileSystemDataSource reads the same
    /// SharedData catalogs the game loads, and DefaultRandomNumberGenerator(seed) makes runs reproducible.
    /// Only the NPC itself is a stub — every service under test is production code.</summary>
    internal sealed class LootPipeline
    {
        public required LootGenerationService LootService { get; init; }
        public required INpcModifierProvider ModifierProvider { get; init; }
        public required IItemDataProvider ItemProvider { get; init; }
        public required ILootConfiguration Configuration { get; init; }
        public required SimEventBus Events { get; init; }
        public required SimMessageBus Messages { get; init; }
        public required IRandomNumberGenerator Rnd { get; init; }

        public static LootPipeline Create(int seed)
        {
            string dataRoot = FindSharedDataRoot();

            var factory = new ItemGameDataFactory();
            var parser = new DataParser(factory);
            var itemProvider = new ItemDataProvider(parser);
            var modifierProvider = new NpcModifierProvider();
            var tableProvider = new LootTableProvider(parser);
            var configurationProvider = new LootConfigurationProvider(parser);

            var dataService = new GameDataService(
                new FileSystemDataSource(dataRoot),
                [itemProvider, modifierProvider, tableProvider, configurationProvider]);
            var loadFailures = new List<string>();
            dataService.LoadFailed += (context, exception) => loadFailures.Add($"{context}: {exception.Message}");
            dataService.LoadAll();
            if (loadFailures.Count > 0)
                throw new InvalidOperationException($"Game data failed to load:\n{string.Join("\n", loadFailures)}");

            var rnd = new DefaultRandomNumberGenerator(seed);
            var events = new SimEventBus();
            var messages = new SimMessageBus();
            messages.Register(new GetLootTableRequestHandler(tableProvider));

            var itemCreation = new ItemCreationService(new ItemEffectProvider(), itemProvider, rnd);
            var lootService = new LootGenerationService(rnd, events, messages, itemCreation, configurationProvider);

            return new LootPipeline
            {
                LootService = lootService,
                ModifierProvider = modifierProvider,
                ItemProvider = itemProvider,
                Configuration = configurationProvider,
                Events = events,
                Messages = messages,
                Rnd = rnd,
            };
        }

        public Task<Dictionary<int, List<TableRecord>>> GetCombinedTableAsync(NpcArchetype archetype) =>
            Messages.SendRequest<GetLootTableRequest, Dictionary<int, List<TableRecord>>>(
                new(archetype.Fraction, archetype.EntityType, archetype.Name));

        /// <summary>Walks up from the test binaries to the Testing project, whose Data/Shared symlink
        /// points at src/SharedData — the same link pattern every game project uses.</summary>
        private static string FindSharedDataRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Data", "Shared");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"Data/Shared symlink not found above {AppContext.BaseDirectory}");
        }
    }
}
