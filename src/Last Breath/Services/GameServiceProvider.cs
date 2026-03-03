namespace LastBreath.Services
{
    using System;
    using System.Collections.Generic;
    using Battle.Source;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;
    using Crafting.Source;
    using Godot;
    using LootGeneration.Source;
    using Microsoft.Extensions.DependencyInjection;
    using Script;
    using Source;
    using Source.Inventory;

    public class GameServiceProvider : IGameServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;

        public static GameServiceProvider Instance { get; } = new();

        private GameServiceProvider()
        {
            _serviceProvider = RegisterServices();
        }

        public T GetService<T>() => _serviceProvider.GetService<T>() ?? throw new NullReferenceException();
        public IEnumerable<T> GetServices<T>() => _serviceProvider.GetServices<T>();
        public T GetKeyedService<T>(string key) => _serviceProvider.GetKeyedService<T>(key) ?? throw new NullReferenceException();

        private ServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>(_ =>
            {
                var instance = new ItemDataProvider();
                instance.LoadData();
                return instance;
            });
            services.AddSingleton(_ =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddSingleton<IGameMessageBus, GameMessageBus>();
            services.AddSingleton<IGameEventBus, GameEventBus>();
            services.AddSingleton<IUiElementProvider, UIElementProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemEffectProvider, ItemEffectProvider>();
            services.AddSingleton<ISettingsHandler, SettingsHandler>();
            services.AddCraftingSystemModuleDependencies();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddBattleSystemModuleDependencies();
            services.AddLootGenerationServices();
            return services.BuildServiceProvider();
        }
    }
}
