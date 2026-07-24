namespace Core.Trade
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// Trade tuning (the Trade catalog). The valuation multipliers turn an item's authored basePrice
    /// into the gold value of a concrete instance; loot-table prices are generator BUDGET units and
    /// have nothing to do with gold. Defaults are placeholders — the JSON is the balance surface.
    /// </summary>
    public class TradeConfig
    {
        /// <summary>Gold multiplier per rarity; a rarity missing from the data falls back to 1.</summary>
        public Dictionary<Rarity, float> RarityMultipliers { get; init; } = new()
        {
            [Rarity.Common] = 1f,
            [Rarity.Uncommon] = 1.5f,
            [Rarity.Rare] = 2.5f,
            [Rarity.Epic] = 4f,
            [Rarity.Legendary] = 7f,
            [Rarity.Unique] = 10f,
            [Rarity.Mythic] = 15f,
        };

        /// <summary>Each sharpening level adds this share of the (rarity-scaled) base to the price.</summary>
        public float UpgradeLevelBonus { get; init; } = 0.1f;

        /// <summary>Extra multiplier for ascended (sealed) gear on top of the Mythic rarity step.</summary>
        public float AscensionMultiplier { get; init; } = 1.5f;

        /// <summary>Share of the valuation a trader pays when the player sells.</summary>
        public float BuybackFactor { get; init; } = 0.4f;

        /// <summary>Bonus-grant chance passed to the minter for shelf equips (mirrors the loot roll).</summary>
        public float RandomEquipEffectChance { get; init; } = 0.15f;

        /// <summary>The player's gold on a fresh game.</summary>
        public int StartingGold { get; init; }
    }

    /// <summary>Consumes the Trade catalog; built-in defaults apply if the JSON is absent.</summary>
    public interface ITradeConfigProvider
    {
        TradeConfig Config { get; }
    }
}
