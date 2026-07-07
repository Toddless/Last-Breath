namespace Core.Data.GameData
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public static class GameDataDependencies
    {
        /// <summary>
        /// Registers the data source rooted at the project's data folder plus the load
        /// orchestrator. Called by the project BOOTSTRAP (modules only register participants),
        /// which also calls <see cref="IGameDataService.LoadAll"/> right after the container is built.
        /// </summary>
        public static IServiceCollection AddGameData(this IServiceCollection services, string dataRootPath)
        {
            services.TryAddSingleton<IGameDataSource>(_ => new GodotDataSource(dataRootPath));
            services.TryAddSingleton<IGameDataService>(provider =>
            {
                var service = new GameDataService(
                    provider.GetRequiredService<IGameDataSource>(),
                    provider.GetServices<IGameDataParticipant>());
                service.LoadFailed += (context, e) => Core.Tracker.TrackException($"Failed to load game data '{context}'", e);
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
    }
}
