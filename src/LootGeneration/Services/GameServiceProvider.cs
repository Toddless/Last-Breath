namespace LootGeneration.Services
{
    using Godot;
    using System;
    using Source;
    using Internal;
    using Utilities;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.MessageBus;
    using Microsoft.Extensions.DependencyInjection;

    internal class GameServiceProvider : IGameServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;
        public static GameServiceProvider Instance { get; } = new();

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
            services.AddSingleton<IGameMessageBus, GameMessageBus>(_ =>
            {
                var instance = new GameMessageBus(this);
                return instance;
            });
            services.AddSingleton<IGameEventBus, GameEventBus>();
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>();
            services.AddSingleton<RandomNumberGenerator>(_ =>
            {
                var rnd = new RandomNumberGenerator();
                rnd.Randomize();
                return rnd;
            });
            services.AddSingleton<INpcModifierProvider, NpcModifierProvider>();
            services.AddSingleton<IItemEffectProvider, ItemEffectProvider>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddLootGenerationServices();
            return services.BuildServiceProvider();
        }
    }
}
