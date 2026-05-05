namespace Core.Data.EquipData
{
    using Newtonsoft.Json;

    public record ItemModifier
    {
        [JsonProperty("parameter")] public string Parameter { get; init; } = string.Empty;
        [JsonProperty("modifierType")] public string ModifierType { get; init; } = string.Empty;
        [JsonProperty("value")] public float Value { get; init; }
        [JsonProperty("weight")] public float Weight { get; init; }
    }
}
