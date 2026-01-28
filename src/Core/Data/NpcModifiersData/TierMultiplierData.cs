namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record TierMultiplierData : NpcModifierData
    {
        [JsonProperty("multiplier")] public float Multiplier { get; init; }
    }
}
