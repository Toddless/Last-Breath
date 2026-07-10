namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>The player's per-faction standing: reputation points plus the level as hysteresis
    /// state (points near a boundary don't tell which side of it the level settled on).
    /// The NPC-vs-NPC matrix is static data and is not persisted.</summary>
    public class FactionRelationsSaveData
    {
        [JsonProperty("playerReputation")] public Dictionary<string, int> PlayerReputation { get; init; } = [];
        [JsonProperty("playerLevels")] public Dictionary<string, string> PlayerLevels { get; init; } = [];
    }
}
