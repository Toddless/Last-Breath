namespace Battle.Services
{
    using Core.Data;
    using Core.Entity;
    using Internal.Npc;
    using Microsoft.Extensions.DependencyInjection;
    using Source;

    /// <summary>Project bootstrap: the shared Core provider + Battle registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(services => services
                .AddBattleSystemModuleDependencies()
                // Project-private bindings: only the bootstrap may know Internal classes
                .AddSingleton<INpcWorldSpawner, BattleNpcWorldSpawner>());
            provider.AddBattleUiElementsFactory();
            return provider;
        }
    }
}
