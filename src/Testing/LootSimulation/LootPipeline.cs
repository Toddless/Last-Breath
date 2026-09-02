namespace LastBreathTest.LootSimulation
{
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Entity.Components;
    using Core.Services;
    using LootGeneration.Internal;
    using LootGeneration.Source;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>The REAL drop pipeline assembled without Godot: FileSystemDataSource reads the same
    /// SharedData catalogs the game loads, and DefaultRandomNumberGenerator(seed) makes runs reproducible.
    /// Only the NPC itself is a stub — every service under test is production code.</summary>
    internal sealed class LootPipeline
    {
        public required LootGenerationService LootService { get; init; }
        public required INpcModifierProvider ModifierProvider { get; init; }

        /// <summary>The chance ladders of issue #223: how many of an NPC's modifier slots actually
        /// fill. Loaded from the same NpcSpawnRolls catalog the game reads, so the simulator's spawn
        /// cascade (see <see cref="LootSimulator.ResolveModifiers"/>) runs on production data.</summary>
        public required Core.Data.INpcSpawnRollsProvider SpawnRolls { get; init; }
        public required IItemDataProvider ItemProvider { get; init; }
        public required ILootConfiguration Configuration { get; init; }
        public required SimEventBus Events { get; init; }
        public required SimMessageBus Messages { get; init; }
        public required IRandomNumberGenerator Rnd { get; init; }
        public required Core.Items.IItemMinter Minter { get; init; }

        /// <summary>The data participants Core registers for every composition, resolved the way a
        /// project resolves them. The stand builds no battle module, so whatever the game keeps
        /// outside its modules has to arrive here through that same registration — otherwise the sim
        /// describes a game with less in it than the one that ships.</summary>
        public required ServiceProvider Shared { get; init; }

        public static LootPipeline Create(int seed)
        {
            string dataRoot = FindSharedDataRoot();

            var rnd = new DefaultRandomNumberGenerator(seed);
            var factory = new ItemGameDataFactory();
            var parser = new DataParser(factory);
            var itemProvider = new ItemDataProvider(parser);
            var modifierProvider = new NpcModifierProvider();
            var spawnRolls = new NpcSpawnRollsProvider();
            var tableProvider = new LootTableProvider(parser);
            var configurationProvider = new LootConfigurationProvider(parser);
            var effectCatalog = new Core.Crafting.CraftingEffectProvider();
            ServiceProvider shared = new ServiceCollection().AddSharedGameDataParticipants().BuildServiceProvider();

            // The combat rules an augment copy draws its numbers around arrive with the shared
            // participants: the stand builds no battle module, and a drop pipeline that cannot mint an
            // augment would report the augment seats of the tables as dropping nothing at all.
            var dataService = new GameDataService(
                new FileSystemDataSource(dataRoot),
                [
                    itemProvider, modifierProvider, spawnRolls, tableProvider, configurationProvider, effectCatalog,
                    .. shared.GetServices<IGameDataParticipant>()
                ]);
            var loadFailures = new List<string>();
            dataService.LoadFailed += (context, exception) => loadFailures.Add($"{context}: {exception.Message}");
            dataService.LoadAll();
            if (loadFailures.Count > 0)
                throw new InvalidOperationException($"Game data failed to load:\n{string.Join("\n", loadFailures)}");

            var events = new SimEventBus();
            var messages = new SimMessageBus();
            messages.Register(new GetLootTableRequestHandler(tableProvider));

            // The same minting seam production DI wires: blueprints -> minter -> facade.
            var materializer = new Core.Modifiers.ModifierMaterializer(rnd);
            var equipMinter = new Core.Items.EquipItemMinter(itemProvider, factory,
                new Core.Items.Grants.GrantFactory(() => null, () => null, () => null), materializer, rnd);
            var augmentCatalog = shared.GetRequiredService<Core.Battle.Abilities.IAbilityAugmentCatalog>();
            var itemMinter = new Core.Items.ItemMinter(itemProvider, equipMinter, itemProvider,
                new Core.Items.AugmentItemMinter(augmentCatalog,
                    new Core.Battle.Abilities.AugmentMinter(augmentCatalog, rnd)));
            var itemCreation = new ItemCreationService(itemProvider, rnd, itemMinter, materializer, effectCatalog,
                new Core.Items.Grants.GrantFactory(() => null, () => null, () => null));
            var draw = new TableRecordDraw(augmentCatalog, rnd);
            var lootService = new LootGenerationService(rnd, events, messages, itemCreation, configurationProvider, draw);

            return new LootPipeline
            {
                LootService = lootService,
                ModifierProvider = modifierProvider,
                SpawnRolls = spawnRolls,
                ItemProvider = itemProvider,
                Configuration = configurationProvider,
                Events = events,
                Messages = messages,
                Rnd = rnd,
                Minter = itemMinter,
                Shared = shared,
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
