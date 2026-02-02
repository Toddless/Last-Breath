namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record RarityUpgradeModifierData : NpcModifierData
    {
        [JsonProperty("multiplier")] public float Multiplier { get; init; }
        [JsonProperty("affectedRarity")] public int[] AffectedRarity { get; init; } = [];
    }
}
