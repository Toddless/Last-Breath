namespace LootGeneration.Services
{
    using Core.Data;
    using Core.Interfaces;
    using Internal;
    using Microsoft.Extensions.DependencyInjection;
    using Source;
    using Utilities;

    /// <summary>Project bootstrap: the shared Core provider + LootGeneration registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } =
            Core.Services.GameServiceProvider.Initialize(RegisterProjectServices);

        private static void RegisterProjectServices(IServiceCollection services)
        {
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddSingleton<IItemDataProvider, ItemDataProvider>();
            services.AddSingleton<INpcModifierProvider, NpcModifierProvider>();
            services.AddSingleton<IItemEffectProvider, ItemEffectProvider>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddLootGenerationServices();
        }
    }
}
