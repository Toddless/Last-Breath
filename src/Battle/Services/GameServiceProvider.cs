namespace Battle.Services
{
    using Godot;
    using System;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using Core.Interfaces.Events;
    using Core.Interfaces.Mediator;
    using System.Collections.Generic;
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
            services.AddSingleton<IMediator, Mediator>();
            services.AddSingleton<RandomNumberGenerator>((_) =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddSingleton<IGameEventBus, GameEventBus>();
            services.AddSingleton<IUIElementProvider, UiElementProvider>();
            services.AddSingleton<IEntityProvider, EntityProvider>();
            return services.BuildServiceProvider();
        }
    }
}
