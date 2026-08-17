namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using Ai.World;
    using Ai.World.Recovery;
    using Ai.World.SmartPoints;
    using Ai.World.Time;
    using Data;
    using Data.GameData;
    using Entity.Components;
    using Events;
    using Godot;
    using Localization;
    using MessageBus;
    using MessageBus.Messages;
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

        /// <summary>The soft read of <see cref="IGameServiceProvider.TryGet{T}"/> for code holding no
        /// provider at all: nothing when no project bootstrapped a composition, where
        /// <see cref="Instance"/> throws and would turn a caller's default into a crash. Same rule as
        /// the instance door — a caller that cannot work without the service asks through
        /// <see cref="Instance"/> instead.</summary>
        public static T? TryGet<T>()
            where T : class => ((IGameServiceProvider?)s_instance)?.TryGet<T>();

        /// <summary>Idempotent: the first configuration wins, later calls return the existing provider.</summary>
        public static IGameServiceProvider Initialize(Action<IServiceCollection> configureProject)
        {
            s_instance ??= new GameServiceProvider(configureProject);
            return s_instance;
        }

        public T GetService<T>()
            where T : notnull => _serviceProvider.GetRequiredService<T>();

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
            services.AddSingleton<ILocalizationProvider, GodotLocalizationProvider>();
            services.AddSharedGameDataParticipants();
            services.AddSingleton<ModifierFormatter>();
            services.AddSingleton<ITextFormatter, ModifierTextFormatter>();
            services.AddSingleton<ContextModifierFormatter>();
            services.AddSingleton<ITextFormatter, ContextModifierTextFormatter>();
            services.AddSingleton<ITextFormatter, ModifierDescriptorTextFormatter>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IKeywordProvider, LocalizationKeywordProvider>();
            services.AddSingleton<IUiElementsManager, UiElementsManager>();
            services.AddSingleton<IUiContextService, UiContextService>();
            services.AddSingleton<IUIWindowPositionStorage, UiWindowPositionStorage>();
            services.AddSingleton<IPlayerAccessor, PlayerAccessor>();
            services.AddSingleton<IRestRecoveryService, RestRecoveryService>();
            services.AddSingleton<ISmartPointRegistry, SmartPointRegistry>();
            services.AddGameDataParticipant<IRecoveryConfigProvider, RecoveryConfigProvider>();

            services.AddSingleton<NotificationService>();
            // The same instance handles the messages: Setup is called on the singleton by the bootstrap
            services.AddSingleton<IMessageHandler<SendNotificationMessageMessage>>(provider => provider.GetRequiredService<NotificationService>());
            services.AddSingleton(_ => CreateRandomizedGenerator());
            services.AddSingleton<IRandomNumberGenerator>(provider =>
                new GodotRandomNumberGenerator(provider.GetRequiredService<RandomNumberGenerator>()));
        }

        private static RandomNumberGenerator CreateRandomizedGenerator()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            return rnd;
        }
    }
}
