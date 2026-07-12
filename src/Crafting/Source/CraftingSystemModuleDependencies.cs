namespace Crafting.Source
{
    using System.Collections.Generic;
    using Core.Crafting;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Events;
    using Core.Interfaces;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Results;
    using Core.Views.UI;
    using EventHandlers;
    using Microsoft.Extensions.DependencyInjection;
    using RequestHandlers;
    using UIElements;

    public static class CraftingSystemModuleDependencies
    {
        public static IServiceCollection AddCraftingSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<ICraftingMastery, CraftingMastery>();
            services.AddSingleton<IModifierMaterializer, ModifierMaterializer>();
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();
            services.AddSingleton<IItemAscender, ItemAscender>();
            services.AddSingleton<CraftingResources>();
            services.AddGameDataParticipant<ICraftingAdditiveProvider, CraftingAdditiveProvider>();

            services.AddTransient<IRequestHandler<CreateEquipItemRequest, IEquipItem?>, CreateEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IRequirement>>, GetEquipItemUpgradeCostRequestHandler>();
            services.AddTransient<IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>, UpgradeEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IRequirement>>, GetEquipItemRecraftModifierCostRequestHandler>();
            services.AddTransient<IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<string>>, RecraftEquipItemModifierRequestHandler>();
            services.AddTransient<IRequestHandler<AscendEquipItemRequest, AscensionResult>, AscendEquipItemRequestHandler>();

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
            uiElementManager.RegisterWindowFactory(typeof(CraftingWindow), () => CraftingWindow.Initialize().Instantiate<CraftingWindow>(), UiContext.World);
        }
    }
}
