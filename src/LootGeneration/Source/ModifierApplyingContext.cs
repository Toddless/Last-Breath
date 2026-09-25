namespace LootGeneration.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Context;
    using Core.Data.LootTable;
    using Core.Enums;
    using Core.Modifiers;

    public class ModifierApplyingContext : IModifierApplyingContext
    {
        public Rarity AtLeast { get; set; } = Rarity.Common;
        public float TierUpgradeChance { get; set; }
        public int TierUpgradeBy { get; set; }
        public float TotalDifficultyMultiplier { get; set; }
        public List<string> GuaranteedItems { get; set; } = [];
        public Dictionary<int, List<TableRecord>> AdditionalItems { get; set; } = [];
        public List<string> AdditionalItemEffects { get; set; } = [];
        public List<IModifier> AdditionalModifiers { get; set; } = [];

        public int TryUpgradeTier(int currentTier, float chance) => chance <= TierUpgradeChance ? Math.Max(0, currentTier - TierUpgradeBy) : currentTier;

        public Rarity TryUpgradeRarity(Rarity currentRarity) => currentRarity <= AtLeast ? currentRarity : AtLeast;
    }
}
