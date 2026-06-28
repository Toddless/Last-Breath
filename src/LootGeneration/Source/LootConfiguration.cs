namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Enums;

    public class LootConfiguration(
        int[] tierPrices,
        float[] baseTierChances,
        float[] baseRarityChances,
        float levelCoefficient,
        float equipItemEffectChance,
        float baseItemModifierMultiplier,
        Dictionary<EntityType, float> baseBudget,
        Dictionary<Rarity, float> rarityMultipliers) : ILootConfiguration
    {
        public int[] TierPrices { get; } = tierPrices;
        public float[] BaseTierChances { get; } = baseTierChances;
        public float[] BaseRarityChances { get; } = baseRarityChances;
        public float LvlCoefficient { get; } = levelCoefficient;
        public float EquipItemEffectChance { get; } = equipItemEffectChance;
        public float ItemModifierMultiplier { get; } = baseItemModifierMultiplier;

        public Dictionary<EntityType, float> BaseBudget { get; } = baseBudget;

        public Dictionary<Rarity, float> RarityMultipliers { get; } = rarityMultipliers;
    }
}
