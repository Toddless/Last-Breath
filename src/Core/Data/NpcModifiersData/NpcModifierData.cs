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
    }
}
