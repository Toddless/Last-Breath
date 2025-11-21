namespace Battle.Services
{
    using System;
    using Core.Interfaces.Data;
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Mediator;
    using Microsoft.Extensions.DependencyInjection;
    using Source;

    internal class GameServiceProvider : IGameServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;
        public static GameServiceProvider Instance { get; } = new();

        private GameServiceProvider()
        {
            _serviceProvider = RegisterServices();
        }

        public T GetService<T>() => _serviceProvider.GetService<T>() ?? throw new NullReferenceException();
        public IEnumerable<T> GetServices<T>() => _serviceProvider.GetServices<T>();

        private ServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMediator, Mediator>();
            services.AddSingleton<IUIElementProvider, UiElementProvider>();
            services.AddSingleton<PlayerReference>();
            services.AddSingleton<IEntityProvider, EntityProvider>();
            services.AddTransient<QueueScheduler>();
            services.AddTransient<CombatScheduler>();
            return services.BuildServiceProvider();
        }
    }
}
