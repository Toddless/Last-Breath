namespace Core.Interfaces
{
    using Enums;
    using Data.LootTable;
    using System.Collections.Generic;

    public interface IModifierApplyingContext
    {
        Rarity AtLeast { get; set; }
        float TierUpgradeChance { get; set; }
        int TierUpgradeBy { get; set; }

        List<string> GuaranteedItems { get; set; }
        Dictionary<int, List<TableRecord>> AdditionalItems { get; set; }
        List<string> AdditionalItemEffects { get; set; }

        int TryUpgradeTier(int currentTier, float chance);
        Rarity TryUpgradeRarity(Rarity currentRarity);
    }
}
