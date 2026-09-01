namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Abilities;
    using CombatRules;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.Items.Grants;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.PassiveTree.Allocation;
    using Core.Save;
    using Core.Services;
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Presentation;
    using RequestHandlers;
    using UIElements;
    using UIElements.PassiveWheel;

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
            services.AddSingleton<IAbilityAugmentBinder, AbilityAugmentBinder>();
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
            // The other half of what the allocation hands out: nodes carrying a passive instead of lines.
            // A singleton beside the unlock service, and after the registry it builds passives through.
            services.AddSingleton<PassiveGrantService>();
            // A data participant since CL-3a: the numbers of every effect are balanced in one catalog,
            // and the registry that builds them is the one place that reads it.
            services.AddGameDataParticipant<IEffectProvider, EffectProvider>();
            // The ability registry builds data-declared behaviours out of it; lazy so registration order
            // stays irrelevant and a sandbox without one still mints every other augment.
            services.AddSingleton<Func<IEffectProvider?>>(sp => sp.GetService<IEffectProvider>);
            // The other half of a card whose effect is named in code: the registry that builds those
            // augments is the one that says what they lay, so the copy in the bag reaches the same canon
            // the seated upgrade reaches. Lazy for the reason above.
            services.AddSingleton<IAugmentLaidEffects>(sp => sp.GetRequiredService<AbilityProvider>());
            services.AddSingleton<Func<IAugmentLaidEffects?>>(sp => sp.GetService<IAugmentLaidEffects>);
            // TryAdd: Main registers both module extensions — whichever runs first wins, the lambdas
            // resolve the providers lazily from the FINAL container, so registration order is irrelevant.
            services.TryAddSingleton<IGrantFactory>(sp => new GrantFactory(
                sp.GetService<ISkillProvider>,
                sp.GetService<IEffectProvider>,
                sp.GetService<IGameEventBus>));
            // // Shared on purpose: NPC looks resolve by NpcId for every spawn path in every project.
             services.AddSingleton<INpcVisualProvider, NpcVisualProvider>();
             services.AddSingleton<ISpawnPointRegistry, SpawnPointRegistry>();

            // TryAdd: the save system (project-level, AddSaveSystem) also offers the scope — the
            // battle services only CONSUME ILoadScope; whichever registration runs first wins.
            services.TryAddSingleton<ILoadScope, LoadScope>();

            // The order of checks an install goes through, written once. A singleton because the socket
            // panel reads it straight — the engine asks whether a drop is allowed inside the frame the
            // pointer moved in, and the request bus answers a frame later — and a preview reading
            // anything but the gate itself would be a second reading of the same rule.
            services.AddSingleton<IAugmentInstallGate>(sp => new AugmentInstallGate(
                sp.GetRequiredService<IAbilitySocketBoard>(),
                sp.GetRequiredService<IAbilityAugmentBinder>(),
                sp.GetService<IInventory>(),
                sp.GetService<IAbilityAugmentCatalog>()));

            // The other thing that moves between the bag and an ability. No binder: an ornament opens an
            // empty socket and cannot come off a full one, so what the abilities WEAR never changes here.
            services.AddSingleton<IOrnamentAttachGate>(sp => new OrnamentAttachGate(
                sp.GetRequiredService<IAbilitySocketBoard>(),
                sp.GetRequiredService<IPlayerAccessor>(),
                sp.GetRequiredService<IOrnamentMinter>(),
                sp.GetService<IInventory>()));

            // The socket sheet and the carried augments beside it: reads, both of them. Everything that
            // MOVES an augment goes through the two request gates below.
            services.AddTransient<IRequestHandler<GetAbilitySocketRowsRequest, IReadOnlyList<AbilitySocketRowView>>, AbilitySocketRowsRequestHandler>();
            services.AddTransient<IRequestHandler<GetCarriedAugmentsRequest, IReadOnlyList<AugmentTrayTileView>>, CarriedAugmentsRequestHandler>();
            // The offer an empty slot makes: the same reader, filtered by the install gate itself.
            services.AddTransient<IRequestHandler<GetAugmentCandidatesRequest, IReadOnlyList<AugmentTrayTileView>>, CarriedAugmentsRequestHandler>();
            // The gates an augment travels between the bag and a slot. They need a bag, which is the
            // game project's — a composition without one owns no augment to move and answers every id
            // as one it does not carry.
            services.AddTransient<IRequestHandler<InstallAugmentRequest, AugmentInstallResult>, InstallAugmentRequestHandler>();
            services.AddTransient<IRequestHandler<ExtractAugmentRequest, AugmentExtractResult>, ExtractAugmentRequestHandler>();
            services.AddTransient<IRequestHandler<AttachOrnamentRequest, OrnamentAttachResult>, AttachOrnamentRequestHandler>();
            services.AddTransient<IRequestHandler<DetachOrnamentRequest, OrnamentDetachResult>, DetachOrnamentRequestHandler>();
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
            // Not read-only (augments are seated from here) — so not available mid-battle.
            uiElementManager.RegisterWindowFactory(typeof(MartialArtMasteryWindow), () => MartialArtMasteryWindow.Initialize().Instantiate<MartialArtMasteryWindow>(), UiContext.World);
            RegisterPassiveWheel(uiElementManager);
            uiElementManager.RegisterPopupFactory(typeof(TextTooltipPopup), () => TextTooltipPopup.Initialize().Instantiate<TextTooltipPopup>());
            // The ability card's own popup; everything that is not an ability stays on the text one above.
            uiElementManager.RegisterPopupFactory(typeof(AbilityTooltipPopup), () => AbilityTooltipPopup.Initialize().Instantiate<AbilityTooltipPopup>());
            uiElementManager.RegisterPopupFactory(typeof(NpcInspectPopup), () => new NpcInspectPopup()); // thin wrapper; the card inside is the shared CharacterBar scene
        }

        /// <summary>
        /// The passive wheel. Registered here rather than in the game project because the scene and its
        /// script live in Battle — the window exports the socket panel, which is a Battle class — and
        /// the game project compiles them through the shared source link. The battle sandbox composes
        /// no tree service; the window resolves it optionally and shows an empty wheel there, the same
        /// way the mastery screen does.
        /// <para>World only, for the same reason as the mastery screen: augments are seated from here.</para>
        /// <para>The scene is not built yet, so the factory is registered only once there is something
        /// to instantiate — opening the wheel before that is a silent no-op instead of a crash.</para>
        /// </summary>
        private static void RegisterPassiveWheel(IUiElementsManager uiElementManager)
        {
            PackedScene? wheel = PassiveWheelWindow.Initialize();
            if (wheel == null) return;

            uiElementManager.RegisterWindowFactory(typeof(PassiveWheelWindow),
                () => wheel.Instantiate<PassiveWheelWindow>(), UiContext.World);
        }
    }
}
