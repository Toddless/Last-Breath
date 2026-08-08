namespace Core.PassiveTree.Rules
{
    /// <summary>
    /// What giving nodes back costs in gold. One formula and one reader: the screen prints the live
    /// price on its button from here and the gate charges from here, so the number the player agreed to
    /// is the number he pays.
    /// <para>A pure calculation — it sees no wallet and no allocation, and asking it costs nothing, which
    /// is what lets a screen ask on every mark.</para>
    /// </summary>
    public interface IPassiveRespecPricing
    {
        /// <param name="nodeCount">How many nodes go back at once.</param>
        /// <param name="masteryLevel">Levels the character EARNED. Never the effective level: that one
        /// carries the bonus levels equipment grants, and a price that moved with a ring swapped on and
        /// off would be a respec bought at whatever the cheapest gear of the moment says.</param>
        int PriceOf(int nodeCount, int masteryLevel);
    }
}
