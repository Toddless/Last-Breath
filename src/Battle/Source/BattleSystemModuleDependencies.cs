namespace Battle.Source
{
    using System.Collections.Generic;
    using Abilities;
    using Core.Ai.World.Skirmish;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Battle.Skills;
    using Core.Data;
    using Core.Entity;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using Npc;
    using RequestHandlers;
    using UIElements;

    public static class BattleSystemModuleDependencies
    {
        public static IServiceCollection AddBattleSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMartialArtMastery, MartialArtMastery>();
            services.AddSingleton<IAbilityProvider, AbilityProvider>();
            services.AddSingleton<IAbilityUnlockService, AbilityUnlockService>();
            services.AddSingleton<INpcProvider, NpcProvider>();
            services.AddSingleton<INpcPopulationService, NpcPopulationService>();
            services.AddSingleton<IFactionRelationService, FactionRelationService>();
            services.AddSingleton<INpcWorldRegistry, NpcWorldRegistry>();
            services.AddSingleton<INpcSkirmishService, NpcSkirmishService>();
            services.AddSingleton<INpcModifierProvider, NpcModifierProvider>();
            services.AddSingleton<INpcBuffProvider, NpcBuffProvider>();
            services.AddSingleton<Core.Ai.World.Time.IWorldClock, World.GameWorldClock>();

            services.AddSingleton<LoadScope>();
            services.AddSingleton<ILoadScope>(sp => sp.GetRequiredService<LoadScope>());
            services.AddSingleton<ISaveStorage>(_ => new SaveStorage(ProjectSettings.GlobalizePath("user://saves")));
            services.AddSingleton(sp => new EquipItemSaveConverter(sp.GetService<ISkillProvider>));
            services.AddSingleton<Save.ISaveGameService, Save.SaveGameService>();
            services.AddSingleton<ISaveManager>(sp =>
            {
                var manager = new SaveManager(sp.GetRequiredService<LoadScope>());
                manager.Register(new WorldClockSaveParticipant(sp.GetRequiredService<Core.Ai.World.Time.IWorldClock>()));
                manager.Register(new FactionRelationsSaveParticipant(sp.GetRequiredService<IFactionRelationService>()));
                manager.Register(new MasterySaveParticipant(sp.GetRequiredService<IMartialArtMastery>()));
                manager.Register(new EquipmentSaveParticipant(sp.GetRequiredService<IPlayerAccessor>(), sp.GetRequiredService<EquipItemSaveConverter>()));
                manager.Register(new AbilityBookSaveParticipant(sp.GetRequiredService<IPlayerAccessor>(), sp.GetRequiredService<IAbilityProvider>()));
                manager.Register(new PlayerVitalsSaveParticipant(sp.GetRequiredService<IPlayerAccessor>()));
                manager.Register(new Save.PlayerPlacementSaveParticipant(sp.GetRequiredService<IPlayerAccessor>()));

                // The npcWorld section needs a project-side NPC factory; a project without one
                // (no world NPCs) simply doesn't write the section.
                if (sp.GetService<INpcWorldSpawner>() is { } spawner)
                    manager.Register(new NpcWorldSaveParticipant(
                        sp.GetRequiredService<INpcWorldRegistry>(),
                        sp.GetRequiredService<INpcProvider>(),
                        sp.GetRequiredService<INpcPopulationService>(),
                        spawner));
                return manager;
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
            uiElementManager.RegisterWindowFactory(typeof(MartialArtMasteryWindow), () => MartialArtMasteryWindow.Initialize().Instantiate<MartialArtMasteryWindow>());
            uiElementManager.RegisterWindowFactory(typeof(AbilityUpgradeWindow), () => AbilityUpgradeWindow.Initialize().Instantiate<AbilityUpgradeWindow>());
            uiElementManager.RegisterWindowFactory(typeof(SaveLoadWindow), () => SaveLoadWindow.Initialize().Instantiate<SaveLoadWindow>());
        }
    }
}
