namespace LastBreath.Services
{
    using Battle.Source;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.UI;
    using Crafting.Source;
    using Inventory;
    using LootGeneration.Source;
    using Microsoft.Extensions.DependencyInjection;
    using Source;
    using UI.View;
    using Utilities;

    /// <summary>Project bootstrap: the shared Core provider + Main registrations. The only place touching the static root.</summary>
    public static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(RegisterProjectServices);
            RegisterUiFactories(provider);
            return provider;
        }

        private static void RegisterProjectServices(IServiceCollection services)
        {
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>();
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
