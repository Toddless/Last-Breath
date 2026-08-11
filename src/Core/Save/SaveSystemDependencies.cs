namespace Core.Save
{
    using Ai.World.Raids;
    using Ai.World.Skirmish;
    using Ai.World.Time;
    using Battle;
    using Reputation;
    using Data;
    using Entity;
    using Godot;
    using Inventory;
    using Items.Grants;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Participants;
    using PassiveTree.Allocation;
    using Services;

    /// <summary>
    /// The save stack is PROJECT infrastructure, not a battle-module concern (module discipline,
    /// Todd 2026-07-24): each project's bootstrap calls this next to its own registrations.
    /// The participant list is the Core-generic set — optional entries follow the GetService
    /// pattern (a project without the service simply doesn't write the section); project-only
    /// sections (Main's narrative, wallet) are registered on top by that project.
    /// </summary>
    public static class SaveSystemDependencies
    {
        public static IServiceCollection AddSaveSystem(this IServiceCollection services)
        {
            // TryAdd: the battle module also offers the scope (its services consume ILoadScope) —
            // whichever registers first wins, both must resolve to the same instance.
            services.TryAddSingleton<LoadScope>();
            services.TryAddSingleton<ILoadScope>(sp => sp.GetRequiredService<LoadScope>());
            services.AddSingleton<ISaveStorage>(_ => new SaveStorage(ProjectSettings.GlobalizePath("user://saves")));
            // The condition catalog is optional the way it is everywhere a line is built: a project that
            // loads no data able to name conditions holds none, and a stored line naming one says so.
            services.AddSingleton(sp => new EquipItemSaveConverter(
                sp.GetRequiredService<IGrantFactory>(),
                sp.GetService<Modifiers.Conditions.IConditionProvider>()));
            services.AddSingleton<ISaveManager>(sp =>
            {
                // The reset service is passed as a factory, not resolved here: it holds the save
                // service, which holds this manager (see SaveManager).
                var manager = new SaveManager(sp.GetRequiredService<LoadScope>(), sp.GetService<Session.ISessionResetService>);
                manager.Register(new WorldClockSaveParticipant(sp.GetRequiredService<IWorldClock>()));
                manager.Register(new FactionRelationsSaveParticipant(sp.GetRequiredService<IFactionRelationService>()));
                // Optional like the npcWorld section: a project without the personal layer doesn't write it.
                if (sp.GetService<IPersonalReputationService>() is { } personalReputation)
                    manager.Register(new PersonalReputationSaveParticipant(personalReputation));
                if (sp.GetService<IRaidService>() is { } raidService)
                    manager.Register(new RaidsSaveParticipant(raidService));
                manager.Register(new MasterySaveParticipant(sp.GetRequiredService<IMartialArtMastery>()));
                // Optional: only projects with the crafting module write the crafting-mastery section
                // (recipe knowledge derives base-recipe availability from this level).
                if (sp.GetService<Crafting.ICraftingMastery>() is { } craftingMastery)
                    manager.Register(new CraftingMasterySaveParticipant(craftingMastery));
                // Optional: only projects with the crafting module write the learned-recipes section.
                if (sp.GetService<Crafting.IRecipeKnowledge>() is { } recipeKnowledge)
                    manager.Register(new RecipeKnowledgeSaveParticipant(recipeKnowledge));
                // Optional: only projects that registered the tree service write the section (the
                // service carries its own catalog, so one lookup answers for both).
                if (sp.GetService<IPassiveTreeService>() is { } passiveTree)
                    manager.Register(new PassiveTreeSaveParticipant(passiveTree));
                manager.Register(new EquipmentSaveParticipant(sp.GetRequiredService<IPlayerAccessor>(), sp.GetRequiredService<EquipItemSaveConverter>()));
                // The bag lives only in projects that have both an inventory and item data (Main);
                // a sandbox without them simply doesn't write the section.
                if (sp.GetService<IInventory>() is { } inventory && sp.GetService<IItemDataProvider>() is { } itemData)
                    manager.Register(new InventorySaveParticipant(inventory, itemData,
                        sp.GetRequiredService<EquipItemSaveConverter>(),
                        // Optional like every seam here: a project that mints no augments holds none
                        // in its bag, and the section is then written without them.
                        sp.GetService<Items.IAugmentItemMinter>()));
                manager.Register(new AbilityBookSaveParticipant(
                    sp.GetRequiredService<IPlayerAccessor>(),
                    sp.GetService<Battle.Abilities.IAbilitySocketBoard>(),
                    // Optional beside the board: the restored arrangement has to reach the abilities,
                    // and a composition holding no slots holds nothing to carry over to them.
                    sp.GetService<Battle.Abilities.IAbilityAugmentBinder>(),
                    // Optional again, and needed only by files written before a copy carried its
                    // rarity: those are drawn once here, from the band their record declares now.
                    sp.GetService<Items.IAugmentItemMinter>()));
                manager.Register(new PlayerVitalsSaveParticipant(sp.GetRequiredService<IPlayerAccessor>()));
                manager.Register(new PlayerPlacementSaveParticipant(sp.GetRequiredService<IPlayerAccessor>()));

                // The npcWorld section needs a project-side NPC factory; a project without one
                // (no world NPCs) simply doesn't write the section.
                if (sp.GetService<INpcWorldSpawner>() is { } spawner)
                    manager.Register(new NpcWorldSaveParticipant(
                        sp.GetRequiredService<INpcWorldRegistry>(),
                        sp.GetRequiredService<INpcProvider>(),
                        // Wild living NPCs restate the modifiers they wear instead of re-rolling them.
                        sp.GetRequiredService<INpcModifierProvider>(),
                        sp.GetRequiredService<INpcPopulationService>(),
                        spawner));
                manager.Register(new SpawnPointsSaveParticipant(sp.GetRequiredService<ISpawnPointRegistry>()));
                return manager;
            });
            return services;
        }
    }
}
