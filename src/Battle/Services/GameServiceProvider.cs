namespace Battle.Services
{
    using Core.Data;
    using Source;

    /// <summary>Project bootstrap: the shared Core provider + Battle registrations. The only place touching the static root.</summary>
    internal static class GameServiceProvider
    {
        public static IGameServiceProvider Instance { get; } = CreateProvider();

        private static IGameServiceProvider CreateProvider()
        {
            var provider = Core.Services.GameServiceProvider.Initialize(services => services.AddBattleSystemModuleDependencies());
            provider.AddBattleUiElementsFactory();
            return provider;
        }
    }
}
