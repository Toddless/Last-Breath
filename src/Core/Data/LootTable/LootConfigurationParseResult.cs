namespace Core.Data.LootTable
{
    using System.Collections.Generic;
    using Enums;

    public record LootConfigurationParseResult(
        int[] TierPrices,
        float[] BaseTierChances,
        float[] BaseRarityChances,
        float LevelCoefficient,
        float EquipItemEffectChance,
        float ItemModifierMultiplier,
        Dictionary<EntityType, float> BaseBudget,
        Dictionary<Rarity, float> RarityMultipliers);
}
