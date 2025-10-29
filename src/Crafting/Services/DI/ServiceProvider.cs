namespace Crafting.Services.DI
{
    using Godot;
    using System;
    using Core.Results;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Events;
    using Crafting.TestResources.Inventory;
    using Core.Interfaces.Mediator.Requests;
    using Microsoft.Extensions.DependencyInjection;
    using Crafting.TestResources;
    using Crafting.Source;
    using Crafting.Source.MediatorHandlers;
    using Crafting.Source.MediatorHandlers.EventHandlers;

    internal class ServiceProvider : IGameServiceProvider
    {
        private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;

        public static ServiceProvider Instance { get; } = new();

        private ServiceProvider()
        {
            _serviceProvider = RegisterServices();
        }

        public T GetService<T>() => _serviceProvider.GetService<T>() ?? throw new NullReferenceException();

        public IEnumerable<T> GetServices<T>() => _serviceProvider.GetServices<T>();

        private Microsoft.Extensions.DependencyInjection.ServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();
            services.AddSingleton<IItemCreator, ItemCreator>();
            services.AddSingleton<IUIElementProvider, UIElementProvider>();
            services.AddSingleton<IUIResourcesProvider, UIResourcesProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IUiMediator, UIMediator>();
            services.AddSingleton<ISystemMediator, SystemMediator>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>((provider) =>
            {
                var instance = new ItemDataProvider("res://TestResources/RecipeAndResources/");
                instance.LoadData();
                return instance;
            });
            services.AddSingleton((provider) =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddSingleton<ICraftingMastery, CraftingMastery>();

            services.AddTransient<IRequestHandler<CreateEquipItemRequest, IEquipItem?>, CreateEquipItemHandler>();
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

            return services.BuildServiceProvider();
        }
    }
}
