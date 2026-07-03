namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Godot;
    using Interfaces.Events;
    using Interfaces.MessageBus;
    using Interfaces.UI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The shared composition root. A project bootstraps it ONCE with its own registrations:
    /// <c>GameServiceProvider.Initialize(services => services.AddMyModuleDependencies())</c>.
    /// Infrastructure every project needs (event/message buses, UI manager, RNG) is registered here;
    /// everything project-specific comes from the configure callback.
    /// </summary>
    public sealed class GameServiceProvider : IGameServiceProvider
    {
        private static GameServiceProvider? s_instance;
        private readonly ServiceProvider _serviceProvider;

        private GameServiceProvider(Action<IServiceCollection> configureProject) =>
            _serviceProvider = BuildProvider(configureProject);

        public static IGameServiceProvider Instance =>
            s_instance ?? throw new InvalidOperationException(
                $"{nameof(GameServiceProvider)} is not initialized. Call {nameof(Initialize)} from the project's bootstrap first.");

        /// <summary>Idempotent: the first configuration wins, later calls return the existing provider.</summary>
        public static IGameServiceProvider Initialize(Action<IServiceCollection> configureProject)
        {
            s_instance ??= new GameServiceProvider(configureProject);
            return s_instance;
        }

        public T GetService<T>()
            where T : notnull => _serviceProvider.GetRequiredService<T>();

        public T GetKeyedService<T>(string key)
            where T : notnull => _serviceProvider.GetRequiredKeyedService<T>(key);

        public IEnumerable<T> GetServices<T>() => _serviceProvider.GetServices<T>();

        private ServiceProvider BuildProvider(Action<IServiceCollection> configureProject)
        {
            var services = new ServiceCollection();
            AddSharedServices(services);
            configureProject(services);
            return services.BuildServiceProvider();
        }

        private void AddSharedServices(IServiceCollection services)
        {
            services.AddSingleton<IGameServiceProvider>(this);
            services.AddSingleton<IGameEventBus, GameEventBus>();
            services.AddSingleton<IGameMessageBus, GameMessageBus>();
            services.AddSingleton<IUiElementsManager, UiElementsManager>();
            services.AddSingleton<IUIWindowPositionStorage, UiWindowPositionStorage>();
            services.AddSingleton(_ => CreateRandomizedGenerator());
        }

        private static RandomNumberGenerator CreateRandomizedGenerator()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            return rnd;
        }
    }
}
