namespace Crafting.Services
{
    using Godot;
    using Source;
    using System;
    using Core.Results;
    using Core.Interfaces;
    using Source.EventHandlers;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using TestResources.Inventory;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Requests;
    using Microsoft.Extensions.DependencyInjection;
    using Source.RequestHandlers;

    internal class GameServiceProvider : IGameServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;

        public static GameServiceProvider Instance
        {
            get
            {
                if (field != null) return field;

                field = new GameServiceProvider();
                return field;
            }
        }

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
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();
            services.AddSingleton<IItemCreator, ItemCreator>();
            services.AddSingleton<IUIElementProvider, UIElementProvider>();
            services.AddSingleton<IUIResourcesProvider, UIResourcesProvider>();
            services.AddSingleton<IInventory, Inventory>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>((_) =>
            {
                var instance = new ItemDataProvider("res://TestResources/RecipeAndResources/");
                instance.LoadData();
                return instance;
            });
            services.AddSingleton((_) =>
            {
                var instance = new RandomNumberGenerator();
                instance.Randomize();
                return instance;
            });
            services.AddSingleton<ICraftingMastery, CraftingMastery>();

            services.AddTransient<IRequestHandler<CreateEquipItemRequest, IEquipItem?>, CreateEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IResourceRequirement>>, GetEquipItemUpgradeCostRequestHandler>();
            services.AddTransient<IRequestHandler<GetTotalItemAmountRequest, Dictionary<string, int>>, GetTotalItemAmountRequestHandler>();
            services.AddTransient<IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>, OpenCraftingItemsWindowRequestHandler>();
            services.AddTransient<IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>, UpgradeEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IResourceRequirement>>, GetEquipItemRecraftModifierCostRequestHandler>();
            services.AddTransient<IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>, RecraftEquipItemModifierRequestHandler>();

            services.AddSingleton<IEventHandler<DestroyItemEvent>, DestroyItemEventHandler>();
            services.AddSingleton<IEventHandler<GainCraftingExpirienceEvent>, GainCraftingExperienceEventHandler>();
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
