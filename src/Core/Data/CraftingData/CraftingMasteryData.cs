namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>Crafting mastery tuning (CraftingMastery catalog). Property defaults mirror the
    /// shipped json so a mastery born before LoadAll (tests, sandboxes) behaves sanely.
    /// Level progress 0..maxLevel linearly lerps every bonus 0..max; consumers apply
    /// final = base × (1 + bonus).</summary>
    public record CraftingMasteryData
    {
        [JsonProperty("maxLevel")] public int MaxLevel { get; init; } = 50;
        [JsonProperty("baseExp")] public int BaseExp { get; init; } = 50;
        [JsonProperty("expFactor")] public float ExpFactor { get; init; } = 1.8f;
        [JsonProperty("bonuses")] public CraftingMasteryBonusesData Bonuses { get; init; } = new();

        /// <summary>The base fraction of used resources refunded on shatter; the resourceReturn
        /// bonus multiplies it.</summary>
        [JsonProperty("baseResourceReturn")] public float BaseResourceReturn { get; init; } = 0.3f;

        /// <summary>Creation rarity weights (enum-name keys, strictly parsed). Mastery shifts the
        /// split through the rarerItem bonus, not through a second weight set.</summary>
        [JsonProperty("rarityWeights")] public Dictionary<string, float> RarityWeights { get; init; } = new()
        {
            ["Legendary"] = 1f,
            ["Epic"] = 10f,
            ["Rare"] = 24f,
            ["Uncommon"] = 65f,
        };

        /// <summary>Experience granted per operation: byRarity value × the mode factor.</summary>
        [JsonProperty("expRewards")] public CraftingMasteryExpRewardsData ExpRewards { get; init; } = new();

        /// <summary>Mastery level (earned + bonus) required before ANY item may ascend.</summary>
        [JsonProperty("ascensionLevelGate")] public int AscensionLevelGate { get; init; } = 35;

        /// <summary>The flat "everything +15%" of ascension: a separate multiplier over every line
        /// of both channels plus a one-time scale of grant payloads.</summary>
        [JsonProperty("ascensionStatBonus")] public float AscensionStatBonus { get; init; } = 0.15f;

        /// <summary>Base chance of the ascension's mythic gift; the mythicModifier channel multiplies it.</summary>
        [JsonProperty("mythicGiftBaseChance")] public float MythicGiftBaseChance { get; init; } = 0.15f;

        /// <summary>Base chance that a crafted item rolls a bonus effect (grant) on creation;
        /// the extraEffect channel multiplies it.</summary>
        [JsonProperty("extraEffectBaseChance")] public float ExtraEffectBaseChance { get; init; } = 0.1f;
    }

    /// <summary>Max bonus of each of the six mastery multipliers (reached at max level).</summary>
    public record CraftingMasteryBonusesData
    {
        [JsonProperty("upgradeChance")] public float UpgradeChance { get; init; } = 2f;
        [JsonProperty("createdValues")] public float CreatedValues { get; init; } = 1.5f;
        [JsonProperty("rarerItem")] public float RarerItem { get; init; } = 1.5f;
        [JsonProperty("extraEffect")] public float ExtraEffect { get; init; } = 1.5f;
        [JsonProperty("mythicModifier")] public float MythicModifier { get; init; } = 1f;
        [JsonProperty("resourceReturn")] public float ResourceReturn { get; init; } = 1.5f;
    }

    public record CraftingMasteryExpRewardsData
    {
        [JsonProperty("byRarity")] public Dictionary<string, int> ByRarity { get; init; } = new()
        {
            ["Common"] = 10,
            ["Uncommon"] = 10,
            ["Rare"] = 20,
            ["Epic"] = 30,
            ["Legendary"] = 40,
            ["Unique"] = 40,
            ["Mythic"] = 40,
        };

        [JsonProperty("modeFactors")] public Dictionary<string, float> ModeFactors { get; init; } = new()
        {
            ["Create"] = 1f,
            ["Upgrade"] = 0.3f,
            ["Recraft"] = 0.3f,
            ["Ascend"] = 1f,
            ["Shatter"] = 0.5f,
        };
    }
}
