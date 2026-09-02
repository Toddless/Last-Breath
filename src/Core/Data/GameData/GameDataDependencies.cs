namespace Core.Data.GameData
{
    using System;
    using Battle;
    using Battle.Abilities;
    using Battle.CombatRules;
    using Entity;
    using Entity.NpcModifiers;
    using Localization;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Services;

    public static class GameDataDependencies
    {
        /// <summary>
        /// The data participants a composition holds whatever modules it builds — catalogs read by
        /// Core itself, so no project can end up without them and no module can end up owning them.
        /// Called by the shared composition root, which is the one thing every project bootstraps.
        /// A participant belongs here when the answers it gives are asked outside the module that
        /// happens to use them most: the augment records are asked wherever an augment is offered,
        /// judged or handled, and that is every project rather than the two that fight.
        /// </summary>
        public static IServiceCollection AddSharedGameDataParticipants(this IServiceCollection services)
        {
            services.AddGameDataParticipant<IParameterFormatProvider, ParameterFormatProvider>();
            services.AddGameDataParticipant<IAbilityAugmentCatalog, AbilityAugmentCatalog>();
            // Shared for the same reason as the augment records: an ornament id turns up in a quest
            // reward, a bag and a save entry, and those are read by compositions holding no battle module.
            services.AddGameDataParticipant<IOrnamentCatalog, OrnamentCatalog>();
            // Shared because the thing that asks it is: every project that can spawn an NPC rolls how
            // many modifiers and abilities that NPC comes out with, and the answer has to be one file.
            services.AddGameDataParticipant<INpcSpawnRollsProvider, NpcSpawnRollsProvider>();
            // Shared because control resistance, arena and effect rules are asked by every project that
            // fights, and a bootstrap-local registration once left one of them without the file.
            services.AddGameDataParticipant<ICombatRulesProvider, CombatRulesProvider>();
            // Shared for the same reason: a buff id rides on an NPC modifier, and the modifier is rolled
            // wherever an NPC is spawned or its corpse is paid out, not only where the fight happens.
            services.AddGameDataParticipant<INpcBuffProvider, NpcBuffProvider>();
            return services;
        }

        /// <summary>
        /// Registers the data source(s) rooted at the project's data folders plus the load
        /// orchestrator. Called by the project BOOTSTRAP (modules only register participants),
        /// which also calls <see cref="IGameDataService.LoadAll"/> right after the container is built.
        /// Several roots layer up (project-local first, then Shared): a catalog is served
        /// wholly by the first root that has it.
        /// </summary>
        public static IServiceCollection AddGameData(this IServiceCollection services, params string[] dataRootPaths)
        {
            services.TryAddSingleton<IGameDataSource>(_ => dataRootPaths.Length == 1
                ? new GodotDataSource(dataRootPaths[0])
                : new CompositeDataSource(Array.ConvertAll(dataRootPaths, IGameDataSource (root) => new GodotDataSource(root))));
            services.TryAddSingleton<IGameDataService>(provider =>
            {
                var service = new GameDataService(
                    provider.GetRequiredService<IGameDataSource>(),
                    provider.GetServices<IGameDataParticipant>());
                service.LoadFailed += (context, e) => Tracker.TrackException($"Failed to load game data '{context}'", e);
                return service;
            });
            return services;
        }

        /// <summary>One singleton, two faces: the provider's service interface + its data-participant registration.</summary>
        public static IServiceCollection AddGameDataParticipant<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService, IGameDataParticipant
        {
            services.AddSingleton<TImplementation>();
            services.AddSingleton<TService>(provider => provider.GetRequiredService<TImplementation>());
            services.AddSingleton<IGameDataParticipant>(provider => provider.GetRequiredService<TImplementation>());
            return services;
        }

        /// <summary>Same two faces for a participant the bootstrap builds itself — the composition that
        /// configures a provider (the catalogs it reads, say) still registers it in one place.</summary>
        public static IServiceCollection AddGameDataParticipant<TService, TImplementation>(
            this IServiceCollection services, Func<IServiceProvider, TImplementation> create)
            where TService : class
            where TImplementation : class, TService, IGameDataParticipant
        {
            services.AddSingleton(create);
            services.AddSingleton<TService>(provider => provider.GetRequiredService<TImplementation>());
            services.AddSingleton<IGameDataParticipant>(provider => provider.GetRequiredService<TImplementation>());
            return services;
        }
    }
}
