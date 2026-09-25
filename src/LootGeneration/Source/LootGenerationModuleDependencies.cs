namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.MessageBus;
    using Microsoft.Extensions.DependencyInjection;

    public static class LootGenerationModuleDependencies
    {
        public static IServiceCollection AddLootGenerationServices(this IServiceCollection services)
        {
            services.AddGameDataParticipant<ILootTableProvider, LootTableProvider>();
            services.AddGameDataParticipant<ILootConfiguration, LootConfigurationProvider>();
            services.AddSingleton<TableRecordDraw>();
            services.AddSingleton<ILootGenerationService, LootGenerationService>();
            services.AddSingleton<ILootOrchestrator, LootOrchestrator>();
            // One orchestrator, two contracts: the save section is handed the floor in file terms and
            // never the scene nodes it is made of.
            services.AddSingleton(sp => (IGroundItemStore)sp.GetRequiredService<ILootOrchestrator>());
            services.AddTransient<IRequestHandler<GetLootTableRequest, Dictionary<int, List<TableRecord>>>, GetLootTableRequestHandler>();
            return services;
        }
    }
}
