namespace Core.Data.NpcModifiersData
{
    using Enums;
    using Newtonsoft.Json;

    public record MinRarityModifierData : NpcModifierData
    {
        [JsonProperty("rarity")] public Rarity Rarity { get; init; }
        [JsonProperty("isUnique")] public bool IsUnique { get; init; }
    }
}
