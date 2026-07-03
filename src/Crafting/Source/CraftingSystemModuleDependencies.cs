namespace Crafting.Source
{
    using System.Collections.Generic;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Interfaces.UI;
    using Core.Modifiers;
    using Core.Results;
    using EventHandlers;
    using Microsoft.Extensions.DependencyInjection;
    using RequestHandlers;
    using UIElements;

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
            services.AddTransient<IMessageHandler<ItemCreatedMessage>, ItemCreatedMessageHandler>();
            services.AddTransient<IMessageHandler<OpenCraftingWindowMessage>, OpenCraftingWindowMessageHandler>();
            return services;
        }

        /// <summary>Shared crafting UI (Source types only — Internal windows are registered by each project's bootstrap).</summary>
        public static void AddCraftingWindowFactories(this IGameServiceProvider provider)
        {
            var uiElementManager = provider.GetService<IUiElementsManager>();
            uiElementManager.RegisterWindowFactory(typeof(CraftingWindow), () => CraftingWindow.Initialize().Instantiate<CraftingWindow>());
            uiElementManager.RegisterWindowFactory(typeof(CraftingItems), () => CraftingItems.Initialize().Instantiate<CraftingItems>());
            uiElementManager.RegisterWindowFactory(typeof(Recipes), () => Recipes.Initialize().Instantiate<Recipes>());
        }
    }
}
