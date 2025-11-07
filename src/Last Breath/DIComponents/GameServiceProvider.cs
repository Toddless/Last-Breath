namespace LastBreath.DIComponents
{
    using Godot;
    using System;
    using Core.Results;
    using Core.Interfaces;
    using Crafting.Source;
    using LastBreath.Script;
    using LastBreath.Script.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using LastBreath.Script.Inventory;
    using Core.Interfaces.Mediator.Events;
    using LastBreath.DIComponents.Mediator;
    using LastBreath.DIComponents.Services;
    using Core.Interfaces.Mediator.Requests;
    using Crafting.Source.MediatorHandlers;
    using LastBreath.DIComponents.MediatorHandlers;
    using Microsoft.Extensions.DependencyInjection;
    using Crafting.Source.MediatorHandlers.EventHandlers;

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
            // i need to figure out how to add all services from another projects 
            var services = new ServiceCollection();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>((provider) =>
            {
                var instance = new ItemDataProvider("res://Data/");
                return instance;
            });
            services.AddSingleton<IUIElementProvider, UIElementProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IUiMediator, UIMediator>();
            services.AddSingleton<ISystemMediator, SystemMediator>();
            services.AddSingleton<IUIResourcesProvider, UIResourcesProvider>();
            services.AddSingleton<IItemCreator, ItemCreator>();
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();
            services.AddSingleton((provider) =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddSingleton<ISettingsHandler, SettingsHandler>();

            services.AddSingleton<ICraftingMastery, CraftingMastery>();

            services.AddTransient<IRequestHandler<CreateEquipItemRequest, IEquipItem?>, CreateEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IResourceRequirement>>, GetEquipItemUpgradeCostRequestHandler>();
            services.AddTransient<IRequestHandler<GetTotalItemAmountRequest, Dictionary<string, int>>, GetTotalItemAmountRequestHandler>();
            services.AddTransient<IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>, OpenCraftingItemsWindowRequestHandler>();
            services.AddTransient<IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>, UpgradeEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IResourceRequirement>>, GetEquipItemRecraftModifierCostRequestHandler>();
            services.AddTransient<IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>, RecraftEquipItemModifierRequestHandler>();

            services.AddSingleton<IEventHandler<DestroyItemEvent>, DestroyItemEventHandler>();
            services.AddSingleton<IEventHandler<GainCraftingExpirienceEvent>, GainCraftingExpirienceEventHandler>();
            services.AddSingleton<IEventHandler<SendNotificationMessageEvent>, SendNotificationMessageEventHandler>();
            services.AddSingleton<IEventHandler<ShowInventorySlotButtonsTooltipEvent>, ShowTooltipEventHandler>();
            services.AddSingleton<IEventHandler<ShowInventoryItemEvent>, ShowInventoryItemEventHandler>();
            services.AddSingleton<IEventHandler<ConsumeResourcesInInventoryEvent>, ConsumeResourcesWithinInventoryEventHandler>();
            services.AddSingleton<IEventHandler<ClearUiElementsEvent>, ClearUiElementsEventHandler>();
            services.AddSingleton<IEventHandler<ItemCreatedEvent>, ItemCreatedEventHandler>();
            services.AddSingleton<IEventHandler<OpenCraftingWindowEvent>, OpenCraftingWindowEventHandler>();


            services.AddSingleton<IEventHandler<OpenInventoryWindowEvent>, OpenWindowEventHandler<OpenInventoryWindowEvent, InventoryWindow>>();
            services.AddSingleton<IEventHandler<OpenQuestWindowEvent>, OpenWindowEventHandler<OpenQuestWindowEvent, QuestsWindow>>();
            services.AddSingleton<IEventHandler<OpenCharacterWindowEvent>, OpenWindowEventHandler<OpenCharacterWindowEvent, CharacterWindow>>();
            services.AddSingleton<IEventHandler<PauseGameEvent>, PauseGameEventHandler>();
            return services.BuildServiceProvider();
        }

    }
}
