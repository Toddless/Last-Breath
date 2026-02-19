namespace Core.Data.EquipData
{
    using Newtonsoft.Json;

    public record EquipItemResource
    {
        [JsonProperty("resourceId")] public string ResourceId { get; init; } = string.Empty;
        [JsonProperty("amount")] public int Amount { get; init; }
    }
}
