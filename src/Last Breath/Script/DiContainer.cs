namespace Playground.Script
{
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public static class DiContainer
    {
        public static ServiceProvider? ServiceProvider
        {
            get; set;
        }

        public static void Configure()
        {
            var provider = new ServiceCollection();

            provider.AddSingleton<IBasedOnRarityLootTable, BasedOnRarityLootTable>();
            provider.AddTransient<RandomNumberGenerator>();

            ServiceProvider = provider.BuildServiceProvider();
        }
    }
}
