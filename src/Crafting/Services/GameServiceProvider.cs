namespace Crafting.Services
{
    using Godot;
    using Source;
    using System;
    using Core.Results;
    using Source.EventHandlers;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using TestResources.Inventory;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Modifiers;
    using Microsoft.Extensions.DependencyInjection;
    using Source.RequestHandlers;

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
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();
            services.AddSingleton<IItemCreator, ItemCreator>();
            services.AddSingleton<IUiElementProvider, UIElementProvider>();
            services.AddSingleton<IUIResourcesProvider, UIResourcesProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>(_ =>
            {
                var instance = new ItemDataProvider("res://TestResources/RecipeAndResources/");
                instance.LoadData();
                return instance;
            });
            services.AddSingleton(_ =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            return services.BuildServiceProvider();
        }
    }
}
