namespace Core.Data.LootTable
{
    using System.Collections.Generic;
    using Core.Data.Schema;
    using Core.Enums;
    using Newtonsoft.Json;

    public record LootConfigurationData
    {
        [JsonProperty("tierPrices")] public int[] TierPrices { get; init; } = [];
        [JsonProperty("baseTierChances")] public float[] BaseTierChances { get; init; } = [];
        [JsonProperty("baseRarityChances")] public float[] BaseRarityChances { get; init; } = [];
        [JsonProperty("levelCoefficient")] public float LevelCoefficient { get; init; }
        [JsonProperty("equipItemEffectChance")] public float EquipItemEffectChance { get; init; }
        [JsonProperty("itemModifierMultiplier")] public float ItemModifierMultiplier { get; init; }
        [JsonProperty("maxItemsPerKill")] public int MaxItemsPerKill { get; init; }

        /// <summary>Gold per leftover budget unit AFTER the quality swap (0 = no gold drops).</summary>
        [JsonProperty("goldPerBudgetUnit")] public float GoldPerBudgetUnit { get; init; }

        /// <summary>Cap on the minted pile (0 = uncapped): overfed NPCs exhaust the item cap AND the
        /// quality swap, and their huge leftover would otherwise mint economy-breaking jackpots.</summary>
        [JsonProperty("maxGoldPerKill")] public int MaxGoldPerKill { get; init; }
        [JsonProperty("baseBudget")] [DictionaryKey(typeof(EntityType))] public Dictionary<string, float> BaseBudget { get; init; } = [];
        [JsonProperty("rarityMultipliers")] [DictionaryKey(typeof(Rarity))] public Dictionary<string, float> RarityMultipliers { get; init; } = [];
    }
}
