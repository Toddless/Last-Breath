namespace Crafting.Services
{
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.UI;
    using Internal;
    using Internal.Inventory;
    using Microsoft.Extensions.DependencyInjection;
    using Source;

    /// <summary>Project bootstrap: the shared Core provider + Crafting registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(RegisterProjectServices);
            provider.AddCraftingWindowFactories();
            RegisterProjectWindows(provider);
            return provider;
        }

        private static void RegisterProjectServices(IServiceCollection services)
        {
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IUIResourcesProvider, UIResourcesProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>();
            services.AddCraftingSystemModuleDependencies();
        }

        /// <summary>Windows living in Internal are project-private and can't be registered by the shared module extension.</summary>
        private static void RegisterProjectWindows(IGameServiceProvider provider)
        {
            var uiElements = provider.GetService<IUiElementsManager>();
            uiElements.RegisterWindowFactory(typeof(InventoryWindow), () => InventoryWindow.Initialize().Instantiate<InventoryWindow>());
        }
    }
}
