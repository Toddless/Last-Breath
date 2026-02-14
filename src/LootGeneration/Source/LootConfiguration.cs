namespace LootGeneration.Source
{
    using Core.Enums;
    using System.Collections.Generic;

    public class LootConfiguration(
        int[] tierPrices,
        float[] baseTierChances,
        float[] baseRarityChances,
        float levelCoefficient,
        float equipItemEffectChance,
        Dictionary<EntityType, float> baseBudget,
        Dictionary<Rarity, float> rarityMultipliers) : ILootConfiguration
    {
        public int[] TierPrices { get; } = tierPrices;
        public float[] BaseTierChances { get; } = baseTierChances;
        public float[] BaseRarityChances { get; } = baseRarityChances;
        public float LvlCoefficient { get; } = levelCoefficient;
        public float EquipItemEffectChance { get; } = equipItemEffectChance;

        public Dictionary<EntityType, float> BaseBudget { get; } = baseBudget;

        public Dictionary<Rarity, float> RarityMultipliers { get; } = rarityMultipliers;
    }
}
