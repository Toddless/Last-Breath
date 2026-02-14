namespace LootGeneration.Source
{
    using Services;
    using Core.Data;
    using Core.Enums;
    using NpcModifiers;
    using Core.Data.LootTable;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Interfaces.MessageBus;
    using Microsoft.Extensions.DependencyInjection;

    public static class LootGenerationModule
    {
        public static IServiceCollection AddLootGenerationServices(this IServiceCollection services)
        {
            services.AddSingleton<ILootTableProvider, LootTableProvider>(_ =>
            {
                var instance = new LootTableProvider();
                instance.LoadData();
                return instance;
            });
            services.AddSingleton<INpcModifierProvider, NpcModifierProvider>(_ =>
            {
                var instance = new NpcModifierProvider(new NpcModifiersFactory());
                instance.LoadDataAsync();
                return instance;
            });
            services.AddSingleton<IItemEffectProvider, ItemEffectProvider>();
            services.AddSingleton<ILootGenerationService, LootGenerationService>(provider =>
                {
                    var instance = new LootGenerationService(
                        provider.GetService<IItemDataProvider>(),
                        provider.GetService<IGameEventBus>(),
                        provider.GetService<IGameMessageBus>(),
                        new LootConfiguration(
                            [500, 350, 100, 25],
                            [0.004f, 0.006f, 0.3f, 0.6f],
                            [0.003f, 0.15f, 0.27f, 0.55f],
                            0.35f,
                            0.15f,
                            new Dictionary<EntityType, float>
                            {
                                [EntityType.Regular] = 5f,
                                [EntityType.Special] = 10f,
                                [EntityType.Elit] = 20f,
                                [EntityType.Unique] = 50f,
                                [EntityType.Boss] = 100f,
                                [EntityType.Archon] = 180f
                            }, new Dictionary<Rarity, float>
                            {
                                [Rarity.Uncommon] = 1f,
                                [Rarity.Rare] = 1.15f,
                                [Rarity.Epic] = 1.3f,
                                [Rarity.Legendary] = 1.45f,
                                [Rarity.Unique] = 2f,
                                [Rarity.Mythic] = 4f,
                            }),
                        provider.GetService<IItemEffectProvider>());
                    return instance;
                }
            );
            services.AddSingleton<ILootOrchestrator, LootOrchestrator>();
            services.AddTransient<IRequestHandler<GetLootTableRequest, Dictionary<int, List<TableRecord>>>, GetLootTableRequestHandler>();
            return services;
        }
    }
}
