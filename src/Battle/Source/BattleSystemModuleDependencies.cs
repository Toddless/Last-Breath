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
    using Core.Items;
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
            // The records an install names are NOT registered here: a composition without this module
            // still has augments to offer, judge and handle, so the catalog is a shared data
            // participant (AddSharedGameDataParticipants) and this module only consumes it.
            // The socket board is filled by the same service that fills the book, and read by the
            // ability-book save section — hence a singleton next to the one that syncs it.
            services.AddSingleton<IAbilitySocketBoard, AbilitySocketBoard>();
            // The one road from the board to the abilities. A singleton because a new playthrough
            // resets through it, and registered before the unlock service, which closes every
            // allocation pass with it.
            services.AddSingleton<AbilityAugmentBinder>();
            services.AddSingleton<IAbilityAugmentBinder>(sp => sp.GetRequiredService<AbilityAugmentBinder>());
            services.AddSingleton<IAbilityUnlockService, AbilityUnlockService>();
            // Shared on purpose: control resistance and arena rules must exist in every project
            // that fights (Main included) — a bootstrap-local registration left Main without them.
            services.AddGameDataParticipant<ICombatRulesProvider, CombatRulesProvider>();
            // Minting an augment — as a copy for a socket or as a thing for the bag — draws its
            // numbers around the band those rules declare, so the two are registered together. A
            // composition without them holds no augment minter, and the item minter treats augment
            // ids as ids it cannot make sense of.
            services.AddSingleton<AugmentMinter>();
            services.AddSingleton<IAugmentItemMinter, AugmentItemMinter>();

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
            // The gates an augment travels between the bag and a slot. They need a bag, which is the
            // game project's — a composition without one owns no augment to move and never sends
            // either request, so the seam is left as a plain dependency instead of an optional one
            // that would answer a question nobody asked.
            services.AddTransient<IRequestHandler<InstallAugmentRequest, AugmentInstallResult>, InstallAugmentRequestHandler>();
            services.AddTransient<IRequestHandler<ExtractAugmentRequest, AugmentExtractResult>, ExtractAugmentRequestHandler>();
            return services;
        }

        /// <summary>
        /// Points the stream a cast rolls on at the engine generator. Deliberately outside
        /// <see cref="AddBattleSystemModuleDependencies"/>: the module is composed by hosts without a
        /// Godot runtime too, and there the engine generator is a native object whose construction takes
        /// the process down. Only a project that boots inside the engine calls this — the assignment
        /// itself builds nothing, the generator is created on the first real cast.
        /// </summary>
        public static void UseEngineCastRandom() => Ability.CastRandomSource = Ability.EngineCastRandom;

        /// <summary>
        /// Points the stream ability delivery rolls on at the engine generator — series lengths, stage
        /// rolls, bounce and jump targets, splash victims, and the generator every attack context of a
        /// cast carries. Outside <see cref="AddBattleSystemModuleDependencies"/> for the same reason as
        /// <see cref="UseEngineCastRandom"/>: hosts without a Godot runtime compose this module too, and
        /// there the engine generator is a native object whose construction takes the process down. The
        /// assignment itself builds nothing — the generator is created on the first real delivery.
        /// </summary>
        public static void UseEngineCombatRandom() => CombatRandom.Source = CombatRandom.EngineCombatRandom;

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
