namespace Battle.Services
{
    using Core.Ai.World.Skirmish;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Reputation;
    using Core.Save;
    using Core.Services;
    using Internal.Npc;
    using Internal.World;
    using Microsoft.Extensions.DependencyInjection;
    using Source;
    using SaveGameService = Internal.Save.SaveGameService;

    /// <summary>Project bootstrap: the shared Core provider + Battle registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(services => services
                .AddBattleSystemModuleDependencies()
                .AddGameData("res://Data/", "res://Data/Shared/")
                // Project-private bindings: only the bootstrap may know Internal classes
                .AddSingleton<INpcWorldSpawner, BattleNpcWorldSpawner>()
                .AddSingleton<IBattleNpcSpawner, BattleSummonSpawner>()
                .AddGameDataParticipant<INpcProvider, NpcProvider>()
                .AddSingleton<INpcPopulationService, NpcPopulationService>()
                .AddSingleton<INpcWorldRegistry, NpcWorldRegistry>()
                .AddGameDataParticipant<INpcBuffProvider, NpcBuffProvider>()
                .AddSingleton<INpcSkirmishService, NpcSkirmishService>()
                .AddGameDataParticipant<INpcModifierProvider, NpcModifierProvider>()
                .AddGameDataParticipant<Core.Ai.World.Time.IWorldClock, GameWorldClock>()
                .AddGameDataParticipant<Core.Ai.World.IPlayerLifecycleConfigProvider, PlayerLifecycleConfigProvider>()
                .AddGameDataParticipant<IFactionRelationService, FactionRelationService>()
                .AddGameDataParticipant<IReputationDeedProcessor, ReputationDeedProcessor>()
                .AddSingleton<ReputationBroadcaster>()
                .AddSingleton<IWitnessQuery, WorldWitnessQuery>()
                .AddGameDataParticipant<IPersonalReputationService, PersonalReputationService>()
                // World facts: the boss-gate reads them ("the twin is finally dead"); the tracker writes them.
                .AddSingleton<Core.Narrative.Facts.IWorldFactsService, Core.Narrative.Facts.WorldFactsService>()
                .AddSingleton<Core.Narrative.Facts.NpcFinalDeathFactTracker>()
                .AddSingleton<ISaveGameService, SaveGameService>());
            provider.AddBattleUiElementsFactory();

            provider.GetService<IGameDataService>().LoadAll();
            provider.GetService<Core.Narrative.Facts.NpcFinalDeathFactTracker>(); // eager: lives on bus subscriptions only
            return provider;
        }
    }
}
