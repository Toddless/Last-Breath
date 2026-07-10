namespace LastBreath.Services
{
    using Battle.Source;
    using Core.Ai.World;
    using Core.Ai.World.Raids;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Events;
    using Core.Interfaces;
    using Core.Inventory;
    using Core.Reputation;
    using Core.Services;
    using Core.Views.UI;
    using Core.Save;
    using Crafting.Source;
    using Inventory;
    using LootGeneration.Source;
    using Microsoft.Extensions.DependencyInjection;
    using Npc;
    using UI.View;
    using World;

    /// <summary>Project bootstrap: the shared Core provider + Main registrations. The only place touching the static root.</summary>
    public static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(RegisterProjectServices);
            RegisterUiFactories(provider);
            provider.GetService<IGameDataService>().LoadAll();
            provider.GetService<ReputationBroadcaster>(); // eager: nobody injects it, it lives on bus subscriptions
            return provider;
        }

        private static void RegisterProjectServices(IServiceCollection services)
        {
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddGameDataParticipant<IItemDataProvider, ItemDataProvider>();
            services.AddGameDataParticipant<IFactionRelationService, FactionRelationService>();
            services.AddGameDataParticipant<IReputationDeedProcessor, ReputationDeedProcessor>();
            services.AddGameDataParticipant<IPersonalReputationService, PersonalReputationService>();
            services.AddGameDataParticipant<IReputationPerkProvider, ReputationPerkProvider>();
            services.AddSingleton<ReputationBroadcaster>();
            services.AddSingleton<INpcWorldRegistry, NpcWorldRegistry>();
            services.AddSingleton<IWitnessQuery, WorldWitnessQuery>();

            // World NPC stack: providers, population, skirmishes, clock, spawner — raids sit on top of it.
            services.AddGameDataParticipant<INpcProvider, NpcProvider>();
            services.AddGameDataParticipant<INpcBuffProvider, NpcBuffProvider>();
            services.AddGameDataParticipant<INpcModifierProvider, NpcModifierProvider>();
            services.AddGameDataParticipant<IWorldClock, GameWorldClock>();
            services.AddGameDataParticipant<IPlayerLifecycleConfigProvider, PlayerLifecycleConfigProvider>();
            services.AddSingleton<INpcPopulationService, NpcPopulationService>();
            services.AddSingleton<Npc.INpcSkirmishService, NpcSkirmishService>();
            services.AddSingleton<INpcWorldSpawner, BattleNpcWorldSpawner>();
            services.AddSingleton<IRaidSpawnRegistry, RaidSpawnRegistry>();
            services.AddGameDataParticipant<IRaidService, RaidService>();
            services.AddSingleton<ISaveGameService, SaveGameService>();
            services.AddGameData("res://Data/", "res://Data/Shared/");
            services.AddTransient<IMessageHandler<OpenWindowMessage>, OpenWindowMessageHandler>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemEffectProvider, ItemEffectProvider>();
            services.AddSingleton<ISettingsHandler, SettingsHandler>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddCraftingSystemModuleDependencies();
            services.AddBattleSystemModuleDependencies();
            services.AddLootGenerationServices();
        }

        private static void RegisterUiFactories(IGameServiceProvider provider)
        {
            var uiElements = provider.GetService<IUiElementsManager>();
            uiElements.RegisterHudFactory(typeof(PlayerHud), () => PlayerHud.Initialize().Instantiate<PlayerHud>());
            uiElements.RegisterWindowFactory(typeof(InventoryWindow), () => InventoryWindow.Initialize().Instantiate<InventoryWindow>());
            provider.AddCraftingWindowFactories();
            provider.AddBattleUiElementsFactory();
        }
    }
}
