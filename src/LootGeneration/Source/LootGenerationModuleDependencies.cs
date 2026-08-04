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
            services.AddTransient<IRequestHandler<GetLootTableRequest, Dictionary<int, List<TableRecord>>>, GetLootTableRequestHandler>();
            return services;
        }
    }
}
