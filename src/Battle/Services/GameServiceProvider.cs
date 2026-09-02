namespace Battle.Services
{
    using Core.Ai.World;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Narrative.Facts;
    using Core.Reputation;
    using Core.Services;
    using Core.Session;
    using Internal.Npc;
    using Internal.World;
    using Microsoft.Extensions.DependencyInjection;
    using SharedUi;
    using Source;

    /// <summary>Project bootstrap: the shared Core provider + Battle registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            // The streams a cast rolls on are chosen here, next to the container that already binds the
            // engine RNG for everything else: the sandbox runs inside Godot, so its casts and their
            // delivery roll on the engine generator too — the domain defaults stay free of the engine
            // for hosts without one.
            BattleSystemModuleDependencies.UseEngineCastRandom();
            BattleSystemModuleDependencies.UseEngineCombatRandom();
            var provider = Core.Services.GameServiceProvider.Initialize(services => services
                .AddBattleSystemModuleDependencies()
                .AddGameData("res://Data/", "res://Data/Shared/")
                // Project-private bindings: only the bootstrap may know Internal classes
                .AddSingleton<INpcWorldSpawner, BattleNpcWorldSpawner>()
                .AddSingleton<IBattleNpcSpawner, BattleSummonSpawner>()
                .AddGameDataParticipant<INpcProvider, NpcProvider>()
                .AddSingleton<INpcPopulationService, NpcPopulationService>()
                .AddSingleton<INpcWorldRegistry, NpcWorldRegistry>()
                .AddSingleton<INpcSkirmishService, NpcSkirmishService>()
                .AddGameDataParticipant<INpcModifierProvider, NpcModifierProvider>()
                .AddGameDataParticipant<IWorldClock, GameWorldClock>()
                .AddGameDataParticipant<IPlayerLifecycleConfigProvider, PlayerLifecycleConfigProvider>()
                .AddGameDataParticipant<IFactionRelationService, FactionRelationService>()
                .AddGameDataParticipant<IReputationDeedProcessor, ReputationDeedProcessor>()
                .AddSingleton<ReputationBroadcaster>()
                .AddSingleton<IWitnessQuery, WorldWitnessQuery>()
                .AddGameDataParticipant<IPersonalReputationService, PersonalReputationService>()
                // World facts: the boss-gate reads them ("the twin is finally dead"); the tracker writes them.
                .AddSingleton<IWorldFactsService, WorldFactsService>()
                .AddSingleton<NpcFinalDeathFactTracker>()
                // Project infrastructure (module discipline): the sandbox composes its own session
                // reset — it no longer rides in the battle module. No save system here on purpose:
                // saving belongs to the game project alone, the sandbox is for isolated tests.
                .AddSessionReset());
            provider.AddBattleUiElementsFactory();
            provider.AddSharedUiFactories();

            provider.GetService<IGameDataService>().LoadAll();
            provider.GetService<NpcFinalDeathFactTracker>(); // eager: lives on bus subscriptions only
            return provider;
        }
    }
}
