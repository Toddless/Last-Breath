namespace Core.Data.NpcModifiersData
{
    using Core.Enums;
    using Newtonsoft.Json;

    public record RarityUpgradeData : NpcModifierData
    {
        [JsonProperty("rarity")] public Rarity Rarity { get; init; }
    }
}
