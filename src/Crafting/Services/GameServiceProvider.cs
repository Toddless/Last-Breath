namespace Crafting.Services
{
    using Godot;
    using Source;
    using System;
    using Internal;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Internal.Inventory;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    internal class GameServiceProvider : IGameServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;

        public static GameServiceProvider Instance
        {
            get
            {
                if (field != null) return field;

                field = new GameServiceProvider();
                return field;
            }
        }

        private GameServiceProvider()
        {
            _serviceProvider = RegisterServices();
        }

        public T GetService<T>() => _serviceProvider.GetService<T>() ?? throw new NullReferenceException();
        public T GetKeyedService<T>(string key) => _serviceProvider.GetKeyedService<T>(key) ?? throw new NullReferenceException();

        public IEnumerable<T> GetServices<T>() => _serviceProvider.GetServices<T>();

        private ServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IGameMessageBus, GameMessageBus>();
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IUiElementsManager, UiElementManager>(_ =>
            {
                var instance = new UiElementManager(this);
                return instance;
            });
            services.AddSingleton<IUIResourcesProvider, UIResourcesProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>();
            services.AddSingleton(_ =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddCraftingSystemModuleDependencies();
            return services.BuildServiceProvider();
        }
    }
}
