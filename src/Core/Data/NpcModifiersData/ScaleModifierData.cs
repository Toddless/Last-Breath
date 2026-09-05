namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record ScaleModifierData : NpcModifierData
    {
        [JsonProperty("scale")] public float Scale { get; init; }
        [JsonProperty("isUnique")] public bool IsUnique { get; init; }
    }
}
