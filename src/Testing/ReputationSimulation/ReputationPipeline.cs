namespace LastBreathTest.ReputationSimulation
{
    using Core.Ai.World.Raids;
    using Core.Data.FactionData;
    using Core.Data.GameData;
    using Core.Data.ReputationData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;
    using Core.Localization;
    using Core.Reputation;
    using Core.Save;
    using Core.Services;
    using Godot;
    using Moq;
    using Newtonsoft.Json;

    /// <summary>
    /// The REAL reputation pipeline assembled without Godot: FileSystemDataSource reads the same
    /// SharedData catalogs the game loads (Factions, ReputationDeeds, ReputationPerks, Raids).
    /// Only the player, the victims and the witness query are stubs — every service under test is
    /// production code with production data. Balance is changed ONLY with these invariants green.
    /// </summary>
    internal sealed class ReputationPipeline
    {
        private readonly Dictionary<Fractions, IFightableNpc> _victims = [];

        public required FactionRelationService Relations { get; init; }
        public required PersonalReputationService Personal { get; init; }
        public required ReputationPerkProvider Perks { get; init; }
        public required GameEventBus Bus { get; init; }
        public required LoadScope LoadScope { get; init; }
        public required ToggleWitnessQuery Witnesses { get; init; }
        public required IPlayer Player { get; init; }
        public required FactionRelationsData FactionData { get; init; }
        public required ReputationDeedsData DeedsData { get; init; }
        public required RaidsData RaidsData { get; init; }

        public static ReputationPipeline Create()
        {
            string dataRoot = FindSharedDataRoot();

            // The broadcaster path localizes faction names; outside Godot the facade gets a pass-through.
            var localization = new Mock<ILocalizationService>();
            localization.Setup(l => l.Localize(It.IsAny<string>())).Returns<string>(key => key);
            Localization.Override(localization.Object);

            var bus = new GameEventBus();
            var loadScope = new LoadScope();
            var witnesses = new ToggleWitnessQuery();

            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.IsAlive).Returns(true);
            player.SetupGet(p => p.IsFighting).Returns(false);
            var accessor = new Mock<IPlayerAccessor>();
            accessor.SetupGet(a => a.Player).Returns(player.Object);

            var relations = new FactionRelationService();
            var personal = new PersonalReputationService(relations, bus);
            var processor = new ReputationDeedProcessor(bus, relations, accessor.Object, loadScope, witnesses, personal);
            var perks = new ReputationPerkProvider(relations);

            var dataService = new GameDataService(new FileSystemDataSource(dataRoot), [relations, personal, processor, perks]);
            var loadFailures = new List<string>();
            dataService.LoadFailed += (context, exception) => loadFailures.Add($"{context}: {exception.Message}");
            dataService.LoadAll();
            if (loadFailures.Count > 0)
                throw new InvalidOperationException($"Game data failed to load:\n{string.Join("\n", loadFailures)}");

            return new ReputationPipeline
            {
                Relations = relations,
                Personal = personal,
                Perks = perks,
                Bus = bus,
                LoadScope = loadScope,
                Witnesses = witnesses,
                Player = player.Object,
                FactionData = ReadCatalogJson<FactionRelationsData>(dataRoot, DataCatalog.Factions, "FactionRelations.json"),
                DeedsData = ReadCatalogJson<ReputationDeedsData>(dataRoot, DataCatalog.ReputationDeeds, "ReputationDeeds.json"),
                RaidsData = ReadCatalogJson<RaidsData>(dataRoot, DataCatalog.Raids, "Raids.json"),
            };
        }

        /// <summary>A player kill of an NPC of the faction, through the real deed pipeline.</summary>
        public void KillNpc(Fractions faction) =>
            Bus.Publish(new EntityDiedEvent(Victim(faction), Player));

        private IFightableNpc Victim(Fractions faction)
        {
            if (_victims.TryGetValue(faction, out var cached)) return cached;

            var npc = new Mock<IFightableNpc>();
            npc.SetupGet(n => n.Fraction).Returns(faction);
            npc.SetupGet(n => n.InstanceId).Returns($"sim-victim-{faction}");
            npc.SetupGet(n => n.Position).Returns(Vector2.Zero);
            return _victims[faction] = npc.Object;
        }

        private static T ReadCatalogJson<T>(string dataRoot, string catalog, string fileName) =>
            JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(dataRoot, catalog, fileName)))
            ?? throw new InvalidOperationException($"Failed to read {catalog}/{fileName}");

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

    internal sealed class ToggleWitnessQuery : IWitnessQuery
    {
        public bool Witnessed { get; set; } = true;

        public bool HasWitness(Vector2 position, float radius, string? excludeInstanceId = null) => Witnessed;
    }
}
