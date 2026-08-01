namespace Battle.Source
{
    using System.Collections.Generic;
    using Abilities;
    using CombatRules;
    using Core.Ai.World.Raids;
    using Core.Ai.World.Recovery;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.SmartPoints;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Crafting;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Events;
    using Core.Inventory;
    using Core.Items.Grants;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.PassiveTree.Allocation;
    using Core.Reputation;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Session;
    using Core.Trade;
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Presentation;
    using RequestHandlers;
    using UIElements;
    using World;

    public static class BattleSystemModuleDependencies
    {
        public static IServiceCollection AddBattleSystemModuleDependencies(this IServiceCollection services)
        {
            // Spelled out instead of AddGameDataParticipant: the mastery hands its earned levels to the
            // passive tree as points, and the tree service belongs to the game project alone — the
            // sandbox resolves null through the lazy accessor and the grant simply goes nowhere.
            services.AddSingleton(sp => new MartialArtMastery(
                sp.GetRequiredService<IGameMessageBus>(),
                sp.GetService<IPassiveTreeService>));
            services.AddSingleton<IMartialArtMastery>(sp => sp.GetRequiredService<MartialArtMastery>());
            services.AddSingleton<IGameDataParticipant>(sp => sp.GetRequiredService<MartialArtMastery>());
            services.AddGameDataParticipant<IAbilityProvider, AbilityProvider>();
            // The socket board is filled by the same service that fills the book, and read by the
            // ability-book save section — hence a singleton next to the one that syncs it.
            services.AddSingleton<IAbilitySocketBoard, AbilitySocketBoard>();
            services.AddSingleton<IAbilityUnlockService, AbilityUnlockService>();
            // Shared on purpose: control resistance and arena rules must exist in every project
            // that fights (Main included) — a bootstrap-local registration left Main without them.
            services.AddGameDataParticipant<ICombatRulesProvider, CombatRulesProvider>();

            services.AddSingleton<ISkillProvider, PassiveSkillProvider>();
            services.AddSingleton<IGrantEffectProvider, GrantEffectProvider>();
            // TryAdd: Main registers both module extensions — whichever runs first wins, the lambdas
            // resolve the providers lazily from the FINAL container, so registration order is irrelevant.
            services.TryAddSingleton<IGrantFactory>(sp => new GrantFactory(
                sp.GetService<ISkillProvider>,
                sp.GetService<IGrantEffectProvider>,
                sp.GetService<IGameEventBus>));
            // // Shared on purpose: NPC looks resolve by NpcId for every spawn path in every project.
             services.AddSingleton<INpcVisualProvider, NpcVisualProvider>();
             services.AddSingleton<ISpawnPointRegistry, SpawnPointRegistry>();

            // Out-of-combat rest: zones (campfires, spawn points) + participants, ticked by
            // NpcWorldDirector. Shared on purpose — both projects fight and rest.
            services.AddGameDataParticipant<IRecoveryConfigProvider, RecoveryConfigProvider>();
            services.AddSingleton<IRestRecoveryService>(sp =>
                new RestRecoveryService(
                    sp.GetRequiredService<IRecoveryConfigProvider>(),
                    sp.GetService<IWorldClock>()));

            // Smart points: claims die with the claimant — Exit never runs for the dead, so the
            // registry is released by the death events instead.
            services.AddSingleton<ISmartPointRegistry>(sp =>
            {
                var registry = new SmartPointRegistry();
                var bus = sp.GetService<IGameEventBus>();
                bus?.Subscribe<EntityDiedEvent>(evnt => registry.Release(evnt.Entity.InstanceId));
                bus?.Subscribe<NpcFinalDeathEvent>(evnt => registry.Release(evnt.InstanceId));
                return registry;
            });
            // TryAdd: the save system (project-level, AddSaveSystem) also offers the scope — the
            // battle services only CONSUME ILoadScope; whichever registration runs first wins.
            services.TryAddSingleton<LoadScope>();
            services.TryAddSingleton<ILoadScope>(sp => sp.GetRequiredService<LoadScope>());

            services.AddTransient<IRequestHandler<GetStanceAbilityRequest, IReadOnlyList<AbilitySlotView>>, GetStanceAbilityRequestHandler>();
            services.AddTransient<IRequestHandler<GetAbilityUpgradeViewRequest, AbilityUpgradeView>, GetAbilityUpgradeViewRequestHandler>();
            services.AddTransient<IRequestHandler<ApplyAbilityUpgradeRequest, AbilityUpgradeView>, ApplyAbilityUpgradeRequestHandler>();
            return services;
        }

        public static void AddBattleUiElementsFactory(this IGameServiceProvider provider)
        {
            var uiElementManager = provider.GetService<IUiElementsManager>();
            uiElementManager.RegisterHudFactory(typeof(BattleHud), () => BattleHud.Initialize().Instantiate<BattleHud>());
            // Not read-only (upgrades apply from here) — so not available mid-battle (design, Todd 2026-07-11).
            uiElementManager.RegisterWindowFactory(typeof(MartialArtMasteryWindow), () => MartialArtMasteryWindow.Initialize().Instantiate<MartialArtMasteryWindow>(), UiContext.World);
            uiElementManager.RegisterWindowFactory(typeof(AbilityUpgradeWindow), () => AbilityUpgradeWindow.Initialize().Instantiate<AbilityUpgradeWindow>(), UiContext.World);
            uiElementManager.RegisterPopupFactory(typeof(TextTooltipPopup), () => TextTooltipPopup.Initialize().Instantiate<TextTooltipPopup>());
            uiElementManager.RegisterPopupFactory(typeof(NpcInspectPopup), () => new NpcInspectPopup()); // thin wrapper; the card inside is the shared CharacterBar scene
        }
    }
}
