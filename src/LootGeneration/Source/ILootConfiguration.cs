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

        /// <summary>Gold minted per unit of the FINAL budget leftover (after the quality swap);
        /// 0 = kills drop no gold. The gold pile rides the normal drop channel as a currency item.</summary>
        float GoldPerBudgetUnit { get; }

        /// <summary>Cap on the minted pile (0 = uncapped) — the anti-jackpot fuse for overfed NPCs.</summary>
        int MaxGoldPerKill { get; }

        Dictionary<EntityType, float> BaseBudget { get; }
        Dictionary<Rarity, float> RarityMultipliers { get; }
    }
}
