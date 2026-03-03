namespace Core.Data.ItemData
{
    using Newtonsoft.Json;

    public record ItemData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("maxStackSize")] public int MaxStackSize { get; init; }
        [JsonProperty("rarity")] public string Rarity { get; init; } = string.Empty;
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];
    }
}
