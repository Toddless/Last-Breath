namespace Core.Data.InfluenceData
{
    using Newtonsoft.Json;

    /// <summary>InfluenceMastery.json: leveling curve and the chance formulas.</summary>
    public record InfluenceMasteryData
    {
        [JsonProperty("maxLevel")] public int MaxLevel { get; init; } = 50;
        [JsonProperty("baseExp")] public int BaseExp { get; init; } = 50;
        [JsonProperty("expFactor")] public float ExpFactor { get; init; } = 1.8f;
        [JsonProperty("speechCheckExp")] public int SpeechCheckExp { get; init; } = 5;
        [JsonProperty("firstTalkExp")] public int FirstTalkExp { get; init; } = 2;
        [JsonProperty("speechCheck")] public SpeechCheckEntry SpeechCheck { get; init; } = new();
        [JsonProperty("questOffer")] public QuestOfferEntry QuestOffer { get; init; } = new();
    }

    /// <summary>chance = clamp(baseChance + (level − difficulty) × chancePerLevel + bonus, min, max).</summary>
    public record SpeechCheckEntry
    {
        [JsonProperty("baseChance")] public float BaseChance { get; init; } = 0.5f;
        [JsonProperty("chancePerLevel")] public float ChancePerLevel { get; init; } = 0.05f;
        [JsonProperty("min")] public float Min { get; init; } = 0.05f;
        [JsonProperty("max")] public float Max { get; init; } = 0.95f;
    }

    /// <summary>Same curve with the tier converted to a difficulty: (tier − 1) × levelsPerTier.
    /// Max 1.0 by default — high Influence makes low-tier offers a certainty.</summary>
    public record QuestOfferEntry
    {
        [JsonProperty("baseChance")] public float BaseChance { get; init; } = 0.4f;
        [JsonProperty("chancePerLevel")] public float ChancePerLevel { get; init; } = 0.04f;
        [JsonProperty("levelsPerTier")] public int LevelsPerTier { get; init; } = 10;
        [JsonProperty("min")] public float Min { get; init; } = 0.05f;
        [JsonProperty("max")] public float Max { get; init; } = 1f;
    }
}
