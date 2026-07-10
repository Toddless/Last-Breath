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
        float ItemModifierMultiplier { get; }

        /// <summary>Hard cap on rolled (non-guaranteed) items per kill; values &lt;= 0 mean no cap.
        /// Leftover budget converts into tier upgrades of already-chosen items.</summary>
        int MaxItemsPerKill { get; }
        Dictionary<EntityType, float> BaseBudget { get; }
        Dictionary<Rarity, float> RarityMultipliers { get; }
    }
}
