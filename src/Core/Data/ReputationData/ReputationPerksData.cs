namespace Core.Data.ReputationData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>ReputationPerks.json: each standing level lists its COMPLETE perk set (no inheritance between levels).</summary>
    public record ReputationPerksData
    {
        [JsonProperty("levels")] public List<ReputationPerkLevelEntry> Levels { get; init; } = [];
    }

    public record ReputationPerkLevelEntry
    {
        [JsonProperty("level")] public string Level { get; init; } = string.Empty;
        [JsonProperty("perks")] public List<ReputationPerkEntry> Perks { get; init; } = [];
    }

    public record ReputationPerkEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("value")] public float Value { get; init; }
    }
}
