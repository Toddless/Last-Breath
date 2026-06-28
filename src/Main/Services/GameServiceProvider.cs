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
    using Core.Interfaces.UI;
    using Crafting.Source;
    using Godot;
    using Inventory;
    using LootGeneration.Source;
    using Microsoft.Extensions.DependencyInjection;
    using Source;
    using Utilities;

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
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>();
            services.AddSingleton(_ =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddSingleton<IUiElementsManager, UiElementsManager>(_ =>
            {
                var instance = new UiElementsManager(this);
                return instance;
            });
            services.AddTransient<IMessageHandler<OpenWindowMessage>, OpenWindowMessageHandler>();
            services.AddSingleton<IGameMessageBus, GameMessageBus>();
            services.AddSingleton<IGameEventBus, GameEventBus>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemEffectProvider, ItemEffectProvider>();
            services.AddSingleton<ISettingsHandler, SettingsHandler>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddCraftingSystemModuleDependencies();
            services.AddBattleSystemModuleDependencies();
            services.AddLootGenerationServices();
            return services.BuildServiceProvider();
        }
    }
}
