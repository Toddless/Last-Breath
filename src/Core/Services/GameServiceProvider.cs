namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.GameData;
    using Events;
    using Godot;
    using Interfaces;
    using MessageBus;
    using Microsoft.Extensions.DependencyInjection;
    using Views.UI;

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
            services.AddSingleton<Localization.ILocalizationProvider, Localization.GodotLocalizationProvider>();
            services.AddGameDataParticipant<Localization.IParameterFormatProvider, Localization.ParameterFormatProvider>();
            services.AddSingleton<Localization.ModifierFormatter>();
            services.AddSingleton<Localization.ITextFormatter, Localization.ModifierTextFormatter>();
            services.AddSingleton<Localization.ContextModifierFormatter>();
            services.AddSingleton<Localization.ITextFormatter, Localization.ContextModifierTextFormatter>();
            services.AddSingleton<Localization.ILocalizationService, Localization.LocalizationService>();
            services.AddSingleton<Localization.IKeywordProvider, Localization.LocalizationKeywordProvider>();
            services.AddSingleton<IUiElementsManager, UiElementsManager>();
            services.AddSingleton<IUIWindowPositionStorage, UiWindowPositionStorage>();
            services.AddSingleton<IPlayerAccessor, PlayerAccessor>();
            services.AddSingleton<NotificationService>();
            // The same instance handles the messages: Setup is called on the singleton by the bootstrap
            services.AddSingleton<IMessageHandler<SendNotificationMessageMessage>>(
                provider => provider.GetRequiredService<NotificationService>());
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
