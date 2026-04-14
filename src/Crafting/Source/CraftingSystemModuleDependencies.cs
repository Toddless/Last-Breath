namespace Crafting.Source
{
    using Core.Data;
    using UIElements;
    using Core.Results;
    using EventHandlers;
    using Core.Modifiers;
    using RequestHandlers;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Microsoft.Extensions.DependencyInjection;

    public static class CraftingSystemModuleDependencies
    {
        public static IServiceCollection AddCraftingSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ICraftingMastery, CraftingMastery>();
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();

            services.AddTransient<IRequestHandler<CreateEquipItemRequest, IEquipItem?>, CreateEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IRequirement>>, GetEquipItemUpgradeCostRequestHandler>();
            services.AddTransient<IRequestHandler<GetTotalItemAmountRequest, Dictionary<string, int>>, GetTotalItemAmountRequestHandler>();
            services.AddTransient<IRequestHandler<OpenCraftingItemsWindowRequest, IEnumerable<string>>, OpenCraftingItemsWindowRequestHandler>();
            services.AddTransient<IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>, UpgradeEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IRequirement>>, GetEquipItemRecraftModifierCostRequestHandler>();
            services.AddTransient<IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>, RecraftEquipItemModifierRequestHandler>();

            services.AddTransient<IMessageHandler<DestroyItemMessage>, DestroyItemMessageHandler>();
            services.AddTransient<IMessageHandler<GainCraftingExpirienceMessage>, GainCraftingExperienceMessageHandler>();
            services.AddTransient<IMessageHandler<ConsumeResourcesInInventoryMessage>, ConsumeResourcesWithinInventoryMessageHandler>();
            services.AddTransient<IMessageHandler<ItemCreatedMessage>, ItemCreatedMessageHandler>();
            services.AddTransient<IMessageHandler<OpenCraftingWindowMessage>, OpenCraftingWindowMessageHandler>();
            return services;
        }

        public static void AddCraftingWindowFactories(this IGameServiceProvider provider)
        {
            var uiElementManager = provider.GetService<IUiElementsManager>();
            uiElementManager.RegisterWindowFactory(typeof(CraftingWindow), () => CraftingWindow.Initialize().Instantiate<CraftingWindow>());
            uiElementManager.RegisterWindowFactory(typeof(Recipes), () => Recipes.Initialize().Instantiate<Recipes>());
        }
    }
}
