namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record NpcModifierData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("npcBuffId")] public string NpcBuffId { get; init; } = string.Empty;
        [JsonProperty("weight")] public float Weight { get; init; }
        [JsonProperty("difficulty")] public float Difficulty { get; init; }
        [JsonProperty("isUnique")] public bool IsUnique { get; init; }

        /// <summary>Stamped by the parser from the section this entry sat in — the section's uniqueScope,
        /// carried down so the factory can hand it to the modifier without re-reading the file.
        /// Not a JSON field of the entry itself.</summary>
        [JsonIgnore] public Enums.NpcUniqueScope UniqueScope { get; init; }
    }
}
