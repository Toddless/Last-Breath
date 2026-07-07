namespace Core.Context
{
    using System.Collections.Generic;
    using Data.LootTable;
    using Enums;
    using Modifiers;

    public interface IModifierApplyingContext
    {
        Rarity AtLeast { get; set; }
        float TierUpgradeChance { get; set; }
        int TierUpgradeBy { get; set; }
        float TotalDifficultyMultiplier { get; set; }
        List<string> GuaranteedItems { get; set; }
        Dictionary<int, List<TableRecord>> AdditionalItems { get; set; }
        List<string> AdditionalItemEffects { get; set; }
        List<IModifier> AdditionalModifiers { get; set; }

        int TryUpgradeTier(int currentTier, float chance);
        Rarity TryUpgradeRarity(Rarity currentRarity);
    }
}
