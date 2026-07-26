namespace Core.Data.EquipData
{
    using Newtonsoft.Json;

    public record BaseStatData
    {
        [JsonProperty("parameter")] public string Parameter { get; init; } = string.Empty;
        [JsonProperty("value")] public ValueRangeData Value { get; init; }
    }
}
