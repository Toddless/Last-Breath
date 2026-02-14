namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Enums;

    public interface ILootConfiguration
    {
        int[] TierPrices { get; }
        float[] BaseTierChances { get; }
        float[] BaseRarityChances { get; }
        float LvlCoefficient { get; }
        float EquipItemEffectChance { get; }
        Dictionary<EntityType, float> BaseBudget { get; }
        Dictionary<Rarity, float> RarityMultipliers { get; }
    }
}
