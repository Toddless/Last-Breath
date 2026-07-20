namespace Battle.Source
{
    using System.Collections.Generic;
    using Abilities;
    using Core.Ai.World.Skirmish;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Session;
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using RequestHandlers;
    using UIElements;

    public static class BattleSystemModuleDependencies
    {
        public static IServiceCollection AddBattleSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMartialArtMastery, MartialArtMastery>();
            services.AddGameDataParticipant<IAbilityProvider, AbilityProvider>();
            services.AddSingleton<IAbilityUnlockService, AbilityUnlockService>();
            // Shared on purpose: control resistance and arena rules must exist in every project
            // that fights (Main included) — a bootstrap-local registration left Main without them.
            services.AddGameDataParticipant<ICombatRulesProvider, CombatRules.CombatRulesProvider>();

            services.AddSingleton<ISkillProvider, PassiveSkillProvider>();
            services.AddSingleton<IGrantEffectProvider, GrantEffectProvider>();
            // TryAdd: Main registers both module extensions — whichever runs first wins, the lambdas
            // resolve the providers lazily from the FINAL container, so registration order is irrelevant.
            services.TryAddSingleton<Core.Items.Grants.IGrantFactory>(sp => new Core.Items.Grants.GrantFactory(
                sp.GetService<ISkillProvider>,
                sp.GetService<IGrantEffectProvider>,
                sp.GetService<Core.Events.IGameEventBus>));
            // Shared on purpose: NPC looks resolve by NpcId for every spawn path in every project.
            services.AddSingleton<Presentation.INpcVisualProvider, Presentation.NpcVisualProvider>();
            services.AddSingleton<ISpawnPointRegistry, SpawnPointRegistry>();

            // Out-of-combat rest: zones (campfires, spawn points) + participants, ticked by
            // NpcWorldDirector. Shared on purpose — both projects fight and rest.
            services.AddGameDataParticipant<Core.Ai.World.Recovery.IRecoveryConfigProvider, World.RecoveryConfigProvider>();
            services.AddSingleton<Core.Ai.World.Recovery.IRestRecoveryService>(sp =>
                new Core.Ai.World.Recovery.RestRecoveryService(
                    sp.GetRequiredService<Core.Ai.World.Recovery.IRecoveryConfigProvider>(),
                    sp.GetService<Core.Ai.World.Time.IWorldClock>()));

            // Smart points: claims die with the claimant — Exit never runs for the dead, so the
            // registry is released by the death events instead.
            services.AddSingleton<Core.Ai.World.SmartPoints.ISmartPointRegistry>(sp =>
            {
                var registry = new Core.Ai.World.SmartPoints.SmartPointRegistry();
                var bus = sp.GetService<Core.Events.IGameEventBus>();
                bus?.Subscribe<Core.Events.EntityDiedEvent>(evnt => registry.Release(evnt.Entity.InstanceId));
                bus?.Subscribe<Core.Events.NpcFinalDeathEvent>(evnt => registry.Release(evnt.InstanceId));
                return registry;
            });
            services.AddSingleton<LoadScope>();
            services.AddSingleton<ILoadScope>(sp => sp.GetRequiredService<LoadScope>());
            services.AddSingleton<ISaveStorage>(_ => new SaveStorage(ProjectSettings.GlobalizePath("user://saves")));
            services.AddSingleton(sp => new EquipItemSaveConverter(sp.GetRequiredService<Core.Items.Grants.IGrantFactory>()));
            services.AddSingleton<ISaveManager>(sp =>
            {
                var manager = new SaveManager(sp.GetRequiredService<LoadScope>());
                manager.Register(new WorldClockSaveParticipant(sp.GetRequiredService<Core.Ai.World.Time.IWorldClock>()));
                manager.Register(new FactionRelationsSaveParticipant(sp.GetRequiredService<IFactionRelationService>()));
                // Optional like the npcWorld section: a project without the personal layer doesn't write it.
                if (sp.GetService<Core.Reputation.IPersonalReputationService>() is { } personalReputation)
                    manager.Register(new PersonalReputationSaveParticipant(personalReputation));
                if (sp.GetService<Core.Ai.World.Raids.IRaidService>() is { } raidService)
                    manager.Register(new RaidsSaveParticipant(raidService));
                manager.Register(new MasterySaveParticipant(sp.GetRequiredService<IMartialArtMastery>()));
                manager.Register(new EquipmentSaveParticipant(sp.GetRequiredService<IPlayerAccessor>(), sp.GetRequiredService<EquipItemSaveConverter>()));
                // The bag lives only in projects that have both an inventory and item data (Main);
                // a sandbox without them simply doesn't write the section.
                if (sp.GetService<Core.Inventory.IInventory>() is { } inventory && sp.GetService<IItemDataProvider>() is { } itemData)
                    manager.Register(new InventorySaveParticipant(inventory, itemData, sp.GetRequiredService<EquipItemSaveConverter>()));
                manager.Register(new AbilityBookSaveParticipant(sp.GetRequiredService<IPlayerAccessor>(), sp.GetRequiredService<IAbilityProvider>()));
                manager.Register(new PlayerVitalsSaveParticipant(sp.GetRequiredService<IPlayerAccessor>()));
                manager.Register(new PlayerPlacementSaveParticipant(sp.GetRequiredService<IPlayerAccessor>()));

                // The npcWorld section needs a project-side NPC factory; a project without one
                // (no world NPCs) simply doesn't write the section.
                if (sp.GetService<INpcWorldSpawner>() is { } spawner)
                    manager.Register(new NpcWorldSaveParticipant(
                        sp.GetRequiredService<INpcWorldRegistry>(),
                        sp.GetRequiredService<INpcProvider>(),
                        sp.GetRequiredService<INpcPopulationService>(),
                        spawner));
                manager.Register(new SpawnPointsSaveParticipant(sp.GetRequiredService<ISpawnPointRegistry>()));
                return manager;
            });

            services.AddSingleton<ISessionResetService>(sp =>
            {
                var session = new SessionResetService(sp.GetRequiredService<LoadScope>());
                // Registration order = reset order; every entry is optional — a sandbox project
                // without the service simply skips it.
                Add<ISaveGameService>(); // a pending load must not leak into the new game
                Add<Core.Narrative.Quests.IQuestLogService>(); // before the facts they evaluate against
                Add<Core.Narrative.Facts.IWorldFactsService>();
                Add<Core.Narrative.Influence.IInfluenceMastery>();
                Add<IMartialArtMastery>();
                Add<Core.Crafting.ICraftingMastery>();
                Add<Core.Inventory.IInventory>();
                Add<IFactionRelationService>();
                Add<Core.Reputation.IPersonalReputationService>();
                Add<Core.Reputation.IReputationDeedProcessor>();
                Add<Core.Ai.World.Raids.IRaidService>();
                Add<Core.Ai.World.Time.IWorldClock>();
                Add<INpcPopulationService>();
                Add<INpcSkirmishService>(); // ghost skirmishes must not outlive the scene's NPCs
                Add<IUiContextService>();
                return session;

                void Add<T>()
                    where T : class
                {
                    if (sp.GetService<T>() is ISessionResettable resettable) session.Register(resettable);
                }
            });

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
        }
    }
}
