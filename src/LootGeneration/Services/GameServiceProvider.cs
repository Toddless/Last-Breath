namespace LootGeneration.Services
{
    using Core.Data;
    using Core.Data.GameData;
    using Core.Modifiers.Conditions;
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
            services.AddConditionCatalog();
            // LootGeneration has no crafting module: the minting seam registers here directly
            // (mirrors CraftingSystemModuleDependencies).
            services.AddSingleton<Core.Modifiers.IModifierMaterializer, Core.Modifiers.ModifierMaterializer>();
            services.AddSingleton<IEquipBlueprintProvider>(provider => provider.GetRequiredService<IItemDataProvider>());
            // Sandbox has no skill/effect registries: null accessors mint inert grants (display works).
            services.AddSingleton<Core.Items.Grants.IGrantFactory>(sp => new Core.Items.Grants.GrantFactory(
                sp.GetService<Core.Battle.Skills.ISkillProvider>,
                sp.GetService<Core.Battle.Abilities.IEffectProvider>,
                sp.GetService<Core.Events.IGameEventBus>));
            services.AddSingleton<Core.Items.IEquipItemMinter, Core.Items.EquipItemMinter>();
            services.AddSingleton<Core.Items.IItemMinter, Core.Items.ItemMinter>();
            services.AddGameDataParticipant<INpcModifierProvider, NpcModifierProvider>();
            // The ItemEffects catalog feeds the drop's bonus-grant roll (payload travels with the entry).
            services.AddGameDataParticipant<Core.Crafting.ICraftingEffectProvider, Core.Crafting.CraftingEffectProvider>();
            services.AddSingleton<IItemCreationService, ItemCreationService>();
            services.AddLootGenerationServices();
            services.AddGameData("res://Data/", "res://Data/Shared/");
        }
    }
}
