namespace LastBreath.DIComponents
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator;
    using Godot;
    using LastBreath.Script;
    using LastBreath.Script.Inventory;
    using Microsoft.Extensions.DependencyInjection;

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

        private ServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IUIElementProvider,UiElementProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IUiMediator, UIMediator>();
            services.AddSingleton<ISystemMediator, SystemMediator>();
            services.AddSingleton((provider) =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddSingleton<ISettingsHandler, SettingsHandler>();
            return services.BuildServiceProvider();
        }

    }
}
