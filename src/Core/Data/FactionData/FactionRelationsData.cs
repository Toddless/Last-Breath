namespace Core.Data.FactionData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>FactionRelations.json: directed faction entries + the player's default standings.</summary>
    public record FactionRelationsData
    {
        [JsonProperty("relations")] public List<FactionRelationEntry> Relations { get; init; } = [];
        [JsonProperty("playerDefaults")] public List<PlayerRelationEntry> PlayerDefaults { get; init; } = [];
    }

    public record FactionRelationEntry
    {
        [JsonProperty("from")] public string From { get; init; } = string.Empty;
        [JsonProperty("to")] public string To { get; init; } = string.Empty;
        [JsonProperty("level")] public string Level { get; init; } = string.Empty;
    }

    public record PlayerRelationEntry
    {
        [JsonProperty("fraction")] public string Fraction { get; init; } = string.Empty;
        [JsonProperty("level")] public string Level { get; init; } = string.Empty;
    }
}
