namespace Core.PassiveTree.Rules
{
    /// <summary>What giving nodes back costs in gold. One formula and one reader — the screen prints the
    /// live price from here and the gate charges from here, so what the player agreed to is what he pays.
    /// A pure calculation with no wallet or allocation, so a screen can ask on every mark.</summary>
    public interface IPassiveRespecPricing
    {
        /// <param name="nodeCount">How many nodes go back at once.</param>
        /// <param name="masteryLevel">Levels EARNED, never effective — effective carries equipment bonus
        /// levels, and the price must not move with a ring swapped on and off.</param>
        int PriceOf(int nodeCount, int masteryLevel);
    }
}
