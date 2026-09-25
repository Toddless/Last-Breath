namespace Core.Battle
{
    using Newtonsoft.Json;

    /// <summary>MartialArtMastery.json: the leveling curve. Levels are counted from zero, so maxLevel
    /// is also the number of level ups a character can earn — and the passive tree budget that follows
    /// from them. Property defaults mirror the shipped json so a mastery born before LoadAll (tests,
    /// sandboxes) levels on the same numbers.</summary>
    public record MartialArtMasteryData
    {
        [JsonProperty("maxLevel")] public int MaxLevel { get; init; } = 50;

        /// <summary>Cost of the first level; every next one is baseExp × level^expFactor.</summary>
        [JsonProperty("baseExp")] public int BaseExp { get; init; } = 50;

        [JsonProperty("expFactor")] public float ExpFactor { get; init; } = 1.8f;
    }
}
