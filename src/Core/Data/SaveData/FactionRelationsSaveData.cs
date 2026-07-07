namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>The player's per-faction standing (faction name → relation level name).
    /// The NPC-vs-NPC matrix is static data and is not persisted.</summary>
    public class FactionRelationsSaveData
    {
        [JsonProperty("playerRelations")] public Dictionary<string, string> PlayerRelations { get; init; } = [];
    }
}
