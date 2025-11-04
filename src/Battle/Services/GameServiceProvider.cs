namespace Battle.Services
{
    using System;
    using Core.Interfaces.Data;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    internal class GameServiceProvider : IGameServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;
        public static GameServiceProvider Instance { get; } = new();

        public GameServiceProvider()
        {
            _serviceProvider = RegisterServices();
        }

        public T GetService<T>() => _serviceProvider.GetService<T>() ?? throw new NullReferenceException();
        public IEnumerable<T> GetServices<T>() => _serviceProvider.GetServices<T>();

        private ServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();

            return services.BuildServiceProvider();
        }
    }
}
