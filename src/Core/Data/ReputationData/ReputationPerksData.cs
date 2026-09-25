namespace Core.Data.ReputationData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>ReputationPerks.json: each standing level lists its COMPLETE perk set (no inheritance between levels).</summary>
    public record ReputationPerksData
    {
        [JsonProperty("levels")] public List<ReputationPerkLevelEntry> Levels { get; init; } = [];
    }

    public record ReputationPerkLevelEntry
    {
        [JsonProperty("level")][EnumOf(typeof(RelationLevel))] public string Level { get; init; } = string.Empty;
        [JsonProperty("perks")] public List<ReputationPerkEntry> Perks { get; init; } = [];
    }

    public record ReputationPerkEntry
    {
        /// <summary>The word a consumer keys its own rule off — the price rule reads Perk_Price_Change and
        /// nothing else answers for it. No catalog declares these: they are the vocabulary of whoever
        /// spends them, and a perk nobody reads is a line that does nothing rather than a broken id.</summary>
        [NotARef]
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("value")] public float Value { get; init; }
    }
}
