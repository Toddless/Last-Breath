namespace Core.Ai.World
{
    using Data.NpcData;
    using Entity.Components;

    /// <summary>
    /// Turns a definition into the post-defeat cycle it asks for. The choice of ending is made here
    /// and nowhere else: the consumer is a scene node, so a branch mixed up at the call site would
    /// compile and quietly hand a peaceful resident the undead fate — here it is testable.
    /// </summary>
    public static class NpcLifecycleFactory
    {
        /// <summary>
        /// Builds the cycle of <see cref="NpcDefinition.LifecycleKind"/> over the config of that kind
        /// (both configs are always present) and the roll stream its lying is counted by.
        /// </summary>
        public static INpcLifecycle Create(NpcDefinition definition, IRandomNumberGenerator rnd) => definition.LifecycleKind switch
        {
            NpcLifecycleKind.Villager => new VillagerLifecycle(definition.VillagerLifecycle, rnd),
            _ => new NpcLifecycle(definition.Lifecycle, rnd),
        };
    }
}
