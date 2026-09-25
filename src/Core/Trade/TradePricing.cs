namespace Core.Trade
{
    using System;
    using Enums;
    using Items;
    using Reputation;

    /// <summary>
    /// Shop prices on top of the raw valuation: the player's standing with the trader's faction
    /// speaks through Perk_Price_Change (negative value = friendlier prices). Buying pays
    /// valuation × (1 + perk); selling earns valuation × buyback × (1 − perk) — a good standing
    /// buys cheaper AND sells dearer. No perk provider (sandbox) = flat prices.
    /// </summary>
    public class TradePricing(
        IItemValuation valuation,
        ITradeConfigProvider configProvider,
        IReputationPerkProvider? perks = null)
    {
        private const string PricePerkId = "Perk_Price_Change";

        public int BuyPrice(IItem item, Fractions traderFaction)
        {
            int value = valuation.Value(item);
            if (value <= 0) return 0;
            return Math.Max(1, (int)MathF.Round(value * (1f + PricePerk(traderFaction))));
        }

        /// <summary>0 = the trader refuses (unpriced item).</summary>
        public int SellPrice(IItem item, Fractions traderFaction)
        {
            int value = valuation.Value(item);
            if (value <= 0) return 0;
            float price = value * configProvider.Config.BuybackFactor * (1f - PricePerk(traderFaction));
            return Math.Max(1, (int)MathF.Round(price));
        }

        private float PricePerk(Fractions faction)
        {
            if (perks == null) return 0f;
            float total = 0f;
            foreach (var perk in perks.GetPerks(faction))
                if (perk.Id == PricePerkId)
                    total += perk.Value;
            return total;
        }
    }
}
