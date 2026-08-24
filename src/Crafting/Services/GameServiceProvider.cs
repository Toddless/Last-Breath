namespace Crafting.Services
{
    using Core.Data;
    using Core.Data.GameData;
    using Core.Inventory;
    using Core.Modifiers.Conditions;
    using Core.Services;
    using Core.Views.UI;
    using Internal;
    using Internal.Inventory;
    using Microsoft.Extensions.DependencyInjection;
    using SharedUi;
    using Source;

    /// <summary>Project bootstrap: the shared Core provider + Crafting registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(RegisterProjectServices);
            provider.AddCraftingWindowFactories();
            provider.AddSharedUiFactories();
            RegisterProjectWindows(provider);
            provider.GetService<IGameDataService>().LoadAll();
            return provider;
        }

        private static void RegisterProjectServices(IServiceCollection services)
        {
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddGameDataParticipant<IItemDataProvider, ItemDataProvider>();
            services.AddConditionCatalog();
            services.AddCraftingSystemModuleDependencies();
            services.AddGameData("res://Internal/Data/", "res://Internal/Data/Shared/");
        }

        /// <summary>Windows living in Internal are project-private and can't be registered by the shared module extension.</summary>
        private static void RegisterProjectWindows(IGameServiceProvider provider)
        {
            var uiElements = provider.GetService<IUiElementsManager>();
            uiElements.RegisterWindowFactory(typeof(InventoryWindow), () => InventoryWindow.Initialize().Instantiate<InventoryWindow>());
        }
    }
}
