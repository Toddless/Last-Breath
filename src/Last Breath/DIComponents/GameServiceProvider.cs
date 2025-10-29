namespace LastBreath.DIComponents
{
    using Godot;
    using System;
    using Core.Interfaces;
    using LastBreath.Script;
    using Core.Interfaces.Data;
    using LastBreath.Script.UI;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using LastBreath.Script.Inventory;
    using Core.Interfaces.Mediator.Events;
    using LastBreath.DIComponents.Mediator;
    using LastBreath.DIComponents.Services;
    using Microsoft.Extensions.DependencyInjection;
    using LastBreath.DIComponents.MediatorHandlers;

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
            services.AddSingleton<IUIElementProvider, UIElementProvider>();
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


            services.AddSingleton<IEventHandler<OpenInventoryWindowEvent>, OpenWindowEventHandler<OpenInventoryWindowEvent, InventoryWindow>>();
            services.AddSingleton<IEventHandler<OpenQuestWindowEvent>, OpenWindowEventHandler<OpenQuestWindowEvent, QuestsWindow>>();
            services.AddSingleton<IEventHandler<OpenCharacterWindowEvent>, OpenWindowEventHandler<OpenCharacterWindowEvent, CharacterWindow>>();
            services.AddSingleton<IEventHandler<PauseGameEvent>, PauseGameEventHandler>();
            return services.BuildServiceProvider();
        }

    }
}
