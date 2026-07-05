namespace Battle.Source
{
    using System.Collections.Generic;
    using Abilities;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Interfaces.UI;
    using Core.Services;
    using Core.Views;
    using Microsoft.Extensions.DependencyInjection;
    using RequestHandlers;
    using UIElements;

    public static class BattleSystemModuleDependencies
    {
        public static IServiceCollection AddBattleSystemModuleDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IMartialArtMastery, MartialArtMastery>();
            services.AddSingleton<IAbilityProvider, AbilityProvider>();
            services.AddSingleton<IAbilityUnlockService, AbilityUnlockService>();

            services.AddTransient<IRequestHandler<GetStanceAbilityRequest, IReadOnlyList<AbilitySlotView>>, GetStanceAbilityRequestHandler>();
            services.AddTransient<IRequestHandler<GetAbilityUpgradeViewRequest, AbilityUpgradeView>, GetAbilityUpgradeViewRequestHandler>();
            services.AddTransient<IRequestHandler<ApplyAbilityUpgradeRequest, AbilityUpgradeView>, ApplyAbilityUpgradeRequestHandler>();
            return services;
        }

        public static void AddBattleUiElementsFactory(this IGameServiceProvider provider)
        {
            var uiElementManager = provider.GetService<IUiElementsManager>();
            uiElementManager.RegisterHudFactory(typeof(BattleHud), () => BattleHud.Initialize().Instantiate<BattleHud>());
            uiElementManager.RegisterWindowFactory(typeof(MartialArtMasteryWindow), () => MartialArtMasteryWindow.Initialize().Instantiate<MartialArtMasteryWindow>());
            uiElementManager.RegisterWindowFactory(typeof(AbilityUpgradeWindow), () => AbilityUpgradeWindow.Initialize().Instantiate<AbilityUpgradeWindow>());
        }
    }
}
