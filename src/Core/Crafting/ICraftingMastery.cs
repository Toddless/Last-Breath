namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    /// <summary>Crafting mastery: level 0..max linearly lerps six bonus channels from 0 to their
    /// data-driven maximums. Every Get*Bonus returns the bonus itself — the consumer applies
    /// final = base × (1 + bonus).</summary>
    public interface ICraftingMastery : IMastery
    {
        /// <summary>Channel 1 — sharpening success: the upgrader multiplies its level curve by (1 + bonus).</summary>
        float GetUpgradeChanceBonus();

        /// <summary>Channel 2 — created item line values: the FINAL multiplier (1 + mastery bonus),
        /// scaled by an optional external bonus. Stamped as the item's PowerMultiplier.</summary>
        float GetCurrentValueMultiplier(float multiplierBonus = 0);

        /// <summary>Channel 3 — rarer created items: mastery's own bonus plus the external
        /// rarityBonus channel (creation runes and the like) reweight the data weights.</summary>
        Dictionary<Rarity, float> GetRarityProbabilities(float rarityBonus = 0);
        Rarity RollRarity(float rarityBonus = 0);

        /// <summary>Channel 4 — extra effect chance, RAW bonus. Applied form is <see cref="GetExtraEffectChance"/>.</summary>
        float GetExtraEffectChanceBonus();

        /// <summary>Channel 4 applied: the final chance a crafted item rolls a bonus effect on
        /// creation = data base chance × (1 + extra effect bonus).</summary>
        float GetExtraEffectChance();

        /// <summary>Channel 5 — mythic modifier chance (ascension gift). Raw bonus; the applied
        /// form is <see cref="GetMythicGiftChance"/>.</summary>
        float GetMythicModifierChanceBonus();

        /// <summary>Ascension gate: (earned + bonus) level has reached the data threshold
        /// (ascensionLevelGate). Below it no item ascends, whatever its state.</summary>
        bool IsAscensionUnlocked { get; }

        /// <summary>The flat ascension bonus ("everything +15%"), from data. Consumers apply ×(1 + bonus).</summary>
        float AscensionStatBonus { get; }

        /// <summary>Channel 5 applied: the final mythic-gift chance =
        /// data base chance × (1 + mythic modifier bonus).</summary>
        float GetMythicGiftChance();

        /// <summary>Channel 6 — shatter refund: the FINAL fraction of used resources returned
        /// (base fraction × (1 + mastery bonus) × (1 + external bonus)).</summary>
        float GetCurrentResourceMultiplier(float resourceBonus = 0);

        /// <summary>Channel 6, RAW: the lerped resource-return bonus itself (no base fraction).
        /// For yields that must start at ×1 with zero mastery — shatter dust uses floor(1 × (1 + bonus)).</summary>
        float GetResourceReturnBonus();

        /// <summary>Experience granted for one crafting operation (rarity value × mode factor, from data).</summary>
        int GetExperienceReward(CraftingMode mode, Rarity rarity);
    }
}
