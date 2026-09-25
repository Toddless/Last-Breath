namespace Battle.Source.UIElements.PassiveWheel
{
    using Core.Battle;
    using Core.PassiveTree.Rules;
    using Core.Trade;

    /// <summary>What a planned return would cost, as one value three places print in their own words.</summary>
    /// <param name="Nodes">How many nodes the plan gives back.</param>
    /// <param name="Gold">What the gate would charge for exactly that set.</param>
    /// <param name="Affordable">Whether the purse covers it. Kept beside the price rather than worked out
    /// by each reader: a button greyed out by one reading and a price coloured by another would
    /// eventually disagree.</param>
    public readonly record struct RespecQuote(int Nodes, int Gold, bool Affordable);

    /// <summary>
    /// The wheel's one reading of what a respec costs. Every price the screen shows — in the node popup
    /// under the cursor, on the confirmation button, in the guard that catches a plan on its way out —
    /// comes from here, and the arithmetic itself comes from <see cref="IPassiveRespecPricing"/>, which
    /// is also what the gate charges by. So the number the player agreed to is the number he pays.
    ///
    /// <para>The level read is the EARNED one: the effective level carries the bonus levels equipment
    /// grants, and a price that moved with a ring swapped on and off would be a respec bought at whatever
    /// the cheapest gear of the moment says.</para>
    ///
    /// <para>Reset tokens are not here yet and will not need this class rewritten when they arrive: the
    /// seam is a VALUE and not a finished sentence, so a token count becomes one more field of
    /// <see cref="RespecQuote"/>, one more branch here and one more placeholder in three templates, while
    /// the spending stays in the gate. Nothing else may price a respec — the compiler cannot hold that,
    /// so it is written down.</para>
    ///
    /// <para>A composition without a wallet or without a pricing (the battle sandbox) quotes nothing it
    /// could charge: the price is zero and nothing is affordable, so the confirmation button stays shut
    /// with a named reason rather than handing out free respecs down there.</para>
    /// </summary>
    public sealed class PassiveRespecQuotes(
        IPassiveRespecPricing? pricing,
        IMartialArtMastery? mastery,
        IWalletService? wallet)
    {
        /// <summary>Whether there is a purse and a price list at all. What tells a refused respec apart
        /// from an unpriceable one.</summary>
        public bool CanCharge => pricing != null && mastery != null && wallet != null;

        public RespecQuote Quote(int nodes)
        {
            if (!CanCharge) return new RespecQuote(nodes, 0, Affordable: false);

            int gold = pricing!.PriceOf(nodes, mastery!.EarnedLevel);
            return new RespecQuote(nodes, gold, wallet!.Gold >= gold);
        }
    }
}
