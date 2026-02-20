namespace Crafting.Source
{
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Results;
    using EventHandlers;
    using Microsoft.Extensions.DependencyInjection;
    using RequestHandlers;

    public static class CraftingSystemModuleDependencies
    {
        public static IServiceCollection AddCraftingSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ICraftingMastery, CraftingMastery>();
            services.AddSingleton<IItemCreator, ItemCreator>();
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();

            services.AddTransient<IRequestHandler<CreateEquipItemRequest, IEquipItem?>, CreateEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IResourceRequirement>>, GetEquipItemUpgradeCostRequestHandler>();
            services.AddTransient<IRequestHandler<GetTotalItemAmountRequest, Dictionary<string, int>>, GetTotalItemAmountRequestHandler>();
            services.AddTransient<IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>, OpenCraftingItemsWindowRequestHandler>();
            services.AddTransient<IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>, UpgradeEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IResourceRequirement>>, GetEquipItemRecraftModifierCostRequestHandler>();
            services.AddTransient<IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>, RecraftEquipItemModifierRequestHandler>();

            services.AddTransient<IEventHandler<DestroyItemEvent>, DestroyItemEventHandler>();
            services.AddTransient<IEventHandler<GainCraftingExpirienceEvent>, GainCraftingExperienceEventHandler>();
            services.AddTransient<IEventHandler<SendNotificationMessageEvent>, SendNotificationMessageEventHandler>();
            services.AddTransient<IEventHandler<ShowInventorySlotButtonsTooltipEvent>, ShowTooltipEventHandler>();
            services.AddTransient<IEventHandler<ShowInventoryItemEvent>, ShowInventoryItemEventHandler>();
            services.AddTransient<IEventHandler<ConsumeResourcesInInventoryEvent>, ConsumeResourcesWithinInventoryEventHandler>();
            services.AddTransient<IEventHandler<ClearUiElementsEvent>, ClearUiElementsEventHandler>();
            services.AddTransient<IEventHandler<ItemCreatedEvent>, ItemCreatedEventHandler>();
            services.AddTransient<IEventHandler<OpenCraftingWindowEvent>, OpenCraftingWindowEventHandler>();
            return services;
        }
    }
}
