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
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Results;
    using Core.Views.UI;
    using Core.Items.Use;
    using EventHandlers;
    using ItemUse;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using RequestHandlers;
    using UIElements;

    public static class CraftingSystemModuleDependencies
    {
        public static IServiceCollection AddCraftingSystemModuleDependencies(this IServiceCollection services)
        {
            // Mastery tuning (curve, six bonus channels, exp rewards) loads from the CraftingMastery catalog.
            services.AddGameDataParticipant<ICraftingMastery, CraftingMastery>();
            services.AddSingleton<IModifierMaterializer, ModifierMaterializer>();
            // The minting seam: blueprints (the project's IItemDataProvider) -> rolled items. One Core
            // implementation for every project, so roll rules can't drift between loot/craft/quests.
            services.AddSingleton<IEquipBlueprintProvider>(provider => provider.GetRequiredService<IItemDataProvider>());
            services.AddSingleton<IEquipItemMinter, EquipItemMinter>();
            services.AddSingleton<IItemMinter, ItemMinter>();
            // TryAdd (see BattleSystemModuleDependencies): both shared modules offer the factory,
            // the accessors resolve lazily — a sandbox without a skill/effect registry mints inert grants.
            services.TryAddSingleton<Core.Items.Grants.IGrantFactory>(sp => new Core.Items.Grants.GrantFactory(
                sp.GetService<Core.Battle.Skills.ISkillProvider>,
                sp.GetService<Core.Battle.Abilities.IGrantEffectProvider>,
                sp.GetService<IGameEventBus>));
            services.AddSingleton<IItemUpgrader, ItemUpgrader>();
            services.AddSingleton<IItemAscender, ItemAscender>();
            services.AddSingleton<CraftingResources>();
            // Recipe knowledge: mastery ladder ∪ scroll learns. The create handler is the gate.
            services.AddSingleton<IRecipeKnowledge>(sp => new RecipeKnowledge(
                sp.GetRequiredService<IItemDataProvider>(),
                sp.GetRequiredService<ICraftingMastery>()));
            services.AddGameDataParticipant<ICraftingAdditiveProvider, CraftingAdditiveProvider>();
            // The "extra effect" pool crafted items roll from (mastery channel 4).
            services.AddGameDataParticipant<ICraftingEffectProvider, CraftingEffectProvider>();

            services.AddTransient<IRequestHandler<CreateEquipItemRequest, IEquipItem?>, CreateEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IRequirement>>, GetEquipItemUpgradeCostRequestHandler>();
            services.AddTransient<IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>, UpgradeEquipItemRequestHandler>();
            services.AddTransient<IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IRequirement>>, GetEquipItemRecraftModifierCostRequestHandler>();
            services.AddTransient<IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<string>>, RecraftEquipItemModifierRequestHandler>();
            services.AddTransient<IRequestHandler<AscendEquipItemRequest, AscensionResult>, AscendEquipItemRequestHandler>();
            // The augment drain. Its minter belongs to the module that fights, so a sandbox composed
            // without that module registers the gate and can never reach it — which is the truth about
            // such a composition: it holds no augments to hand over either.
            services.AddTransient<IRequestHandler<ConvertAugmentsRequest, AugmentConversionResult>, ConvertAugmentsRequestHandler>();

            services.AddTransient<IMessageHandler<DestroyItemMessage>, DestroyItemMessageHandler>();
            // The item-use seam: the shared channel + dispatch live here with the tooltip button;
            // future consumable modules only register additional IItemUseBehavior implementations.
            services.AddSingleton<IItemUseBehavior, LearnRecipeUseBehavior>();
            services.AddSingleton<IItemUseService>(sp => new ItemUseService(sp.GetServices<IItemUseBehavior>()));
            services.AddTransient<IMessageHandler<UseItemMessage>, UseItemMessageHandler>();
            services.AddTransient<IMessageHandler<GainCraftingExpirienceMessage>, GainCraftingExperienceMessageHandler>();
            services.AddTransient<IMessageHandler<OpenCraftingWindowMessage>, OpenCraftingWindowMessageHandler>();
            return services;
        }

        /// <summary>Shared crafting UI (Source types only — Internal windows are registered by each project's bootstrap).</summary>
        public static void AddCraftingWindowFactories(this IGameServiceProvider provider)
        {
            var uiElementManager = provider.GetService<IUiElementsManager>();
            uiElementManager.RegisterWindowFactory(typeof(CraftingWindow), () => CraftingWindow.Initialize().Instantiate<CraftingWindow>(), UiContext.World);
            uiElementManager.RegisterPopupFactory(typeof(ResourcePickerPopup), () => ResourcePickerPopup.Initialize().Instantiate<ResourcePickerPopup>());
        }
    }
}
