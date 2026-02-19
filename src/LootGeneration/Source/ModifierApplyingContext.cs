namespace LootGeneration.Source
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Data.LootTable;
    using System.Collections.Generic;

    public class ModifierApplyingContext : IModifierApplyingContext
    {
        public Rarity AtLeast { get; set; } = Rarity.Uncommon;
        public float TierUpgradeChance { get; set; }
        public int TierUpgradeBy { get; set; }
        public List<string> GuaranteedItems { get; set; } = [];
        public Dictionary<int, List<TableRecord>> AdditionalItems { get; set; } = [];
        public List<string> AdditionalItemEffects { get; set; } = [];

        public int TryUpgradeTier(int currentTier, float chance) => chance <= TierUpgradeChance ? Math.Max(0, currentTier - TierUpgradeBy) : currentTier;

        public Rarity TryUpgradeRarity(Rarity currentRarity) => currentRarity < AtLeast ? currentRarity : AtLeast;
    }
}
