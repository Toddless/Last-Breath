namespace Core.Session
{
    using Ai.World.Raids;
    using Ai.World.Skirmish;
    using Ai.World.Time;
    using Battle;
    using Crafting;
    using Entity;
    using Inventory;
    using Microsoft.Extensions.DependencyInjection;
    using Narrative.Facts;
    using Narrative.Influence;
    using Narrative.Quests;
    using PassiveTree.Allocation;
    using Reputation;
    using Save;
    using Services;
    using Trade;
    using Views.UI;

    /// <summary>
    /// Session reset is PROJECT infrastructure, not a battle-module concern (module discipline,
    /// Todd 2026-07-24 — this closes the old "НЕ зависимость Battle" TODO): each project's
    /// bootstrap calls this. Registration order = reset order; every entry is optional — a
    /// project without the service simply skips it.
    /// </summary>
    public static class SessionResetDependencies
    {
        public static IServiceCollection AddSessionReset(this IServiceCollection services)
        {
            services.AddSingleton<ISessionResetService>(sp =>
            {
                var session = new SessionResetService(sp.GetRequiredService<LoadScope>());
                Add<ISaveGameService>(); // a pending load must not leak into the new game
                Add<IQuestLogService>(); // before the facts they evaluate against
                Add<IWorldFactsService>();
                Add<IInfluenceMastery>();
                Add<IMartialArtMastery>();
                Add<IPassiveTreeService>(); // after the mastery that grants the points its allocation spends
                Add<ICraftingMastery>();
                Add<IRecipeKnowledge>(); // scroll-learned recipes are per-playthrough
                Add<IInventory>();
                Add<IWalletService>(); // gold must not leak between playthroughs
                Add<ITraderService>(); // shelves and restock timers are per-playthrough
                Add<IFactionRelationService>();
                Add<IPersonalReputationService>();
                Add<IReputationDeedProcessor>();
                Add<IRaidService>();
                Add<IWorldClock>();
                Add<INpcPopulationService>();
                Add<INpcSkirmishService>(); // ghost skirmishes must not outlive the scene's NPCs
                Add<IUiContextService>();
                return session;

                void Add<T>()
                    where T : class
                {
                    if (sp.GetService<T>() is ISessionResettable resettable) session.Register(resettable);
                }
            });
            return services;
        }
    }
}
