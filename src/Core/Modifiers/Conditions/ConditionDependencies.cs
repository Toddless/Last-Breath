namespace Core.Modifiers.Conditions
{
    using Data.GameData;
    using Microsoft.Extensions.DependencyInjection;

    public static class ConditionDependencies
    {
        /// <summary>
        /// The condition catalog with the shipped predicate set: one factory registration per type, the
        /// record parser over them and the provider that keys definitions by id. A project adding a
        /// factory of its own registers it as <see cref="IConditionFactory"/> and it joins the same
        /// registry — the parser takes whatever the container holds.
        /// <para>
        /// Registered by every project that loads data able to carry a condition id: the item modifier
        /// pools (a bootstrap with <see cref="Data.IDataParser"/>) and the passive tree (a bootstrap with
        /// <see cref="PassiveTree.IPassiveTreeProvider"/>). A bootstrap holding neither has nothing to
        /// resolve ids for, and registering the catalog there only makes it read files nobody asks about.
        /// </para>
        /// </summary>
        public static IServiceCollection AddConditionCatalog(this IServiceCollection services)
        {
            foreach (var factory in ConditionParser.BuiltInFactories())
                services.AddSingleton(factory);

            services.AddSingleton<ConditionParser>();
            return services.AddGameDataParticipant<IConditionProvider, ConditionProvider>();
        }
    }
}
