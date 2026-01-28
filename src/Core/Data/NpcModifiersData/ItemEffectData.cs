namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record ItemEffectData : NpcModifierData
    {
        [JsonProperty("effectId")] public string EffectId { get; init; } = string.Empty;
    }
}
