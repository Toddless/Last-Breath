namespace Core.Data.LootTable
{
    using System.Collections.Generic;
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
        [JsonProperty("baseBudget")] public Dictionary<string, float> BaseBudget { get; init; } = [];
        [JsonProperty("rarityMultipliers")] public Dictionary<string, float> RarityMultipliers { get; init; } = [];
    }
}
