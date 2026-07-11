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
    using Core.Views;
    using Core.Views.UI;
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using RequestHandlers;
    using UIElements;

    public static class BattleSystemModuleDependencies
    {
        public static IServiceCollection AddBattleSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMartialArtMastery, MartialArtMastery>();
            services.AddGameDataParticipant<IAbilityProvider, AbilityProvider>();
            services.AddSingleton<IAbilityUnlockService, AbilityUnlockService>();

            services.AddSingleton<ISkillProvider, PassiveSkillProvider>();
            services.AddSingleton<ISpawnPointRegistry, SpawnPointRegistry>();
            services.AddSingleton<LoadScope>();
            services.AddSingleton<ILoadScope>(sp => sp.GetRequiredService<LoadScope>());
            services.AddSingleton<ISaveStorage>(_ => new SaveStorage(ProjectSettings.GlobalizePath("user://saves")));
            services.AddSingleton(sp => new EquipItemSaveConverter(sp.GetService<ISkillProvider>));
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
        }
    }
}
