namespace Battle.Source
{
    using Core.Data;
    using Core.Interfaces.Battle;
    using Core.Interfaces.UI;
    using Microsoft.Extensions.DependencyInjection;
    using UIElements;

    public static class BattleSystemModuleDependencies
    {
        public static IServiceCollection AddBattleSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMartialArtMastery, MartialArtMastery>();
            return services;
        }

        public static void AddBattleUiElementsFactory(this IGameServiceProvider provider)
        {
            var uiElementManager = provider.GetService<IUiElementsManager>();
            uiElementManager.RegisterHudFactory(typeof(BattleHud), () => BattleHud.Initialize().Instantiate<BattleHud>());
            uiElementManager.RegisterWindowFactory(typeof(MartialArtMastery), () => MartialArtMasteryWindow.Initialize().Instantiate<MartialArtMasteryWindow>());
        }
    }
}
