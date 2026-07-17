namespace LootGeneration.Services
{
    using Core.Data;
    using Core.Data.GameData;
    using Core.Services;
    using Internal;
    using Microsoft.Extensions.DependencyInjection;
    using Source;

    /// <summary>Project bootstrap: the shared Core provider + LootGeneration registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(RegisterProjectServices);
            provider.GetService<IGameDataService>().LoadAll();
            return provider;
        }

        private static void RegisterProjectServices(IServiceCollection services)
        {
            services.AddSingleton<IItemGameDataFactory, ItemGameDataFactory>();
            services.AddSingleton<IDataParser, DataParser>();
            services.AddGameDataParticipant<IItemDataProvider, ItemDataProvider>();
            // LootGeneration has no crafting module: the minting seam registers here directly
            // (mirrors CraftingSystemModuleDependencies).
            services.AddSingleton<Core.Modifiers.IModifierMaterializer, Core.Modifiers.ModifierMaterializer>();
            services.AddSingleton<IEquipBlueprintProvider>(provider => provider.GetRequiredService<IItemDataProvider>());
            services.AddSingleton<Core.Items.IEquipItemMinter, Core.Items.EquipItemMinter>();
            services.AddSingleton<Core.Items.IItemMinter, Core.Items.ItemMinter>();
            services.AddGameDataParticipant<INpcModifierProvider, NpcModifierProvider>();
            services.AddSingleton<IItemEffectProvider, ItemEffectProvider>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddLootGenerationServices();
            services.AddGameData("res://Data/", "res://Data/Shared/");
        }
    }
}
