namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record TierUpgradeData : NpcModifierData
    {
        [JsonProperty("isUnique")] public bool IsUnique { get; init; }
        [JsonProperty("tierUpgradeChance")] public float TierUpgradeChance { get; init; }
        [JsonProperty("upgradeBy")] public int UpgradeBy { get; init; }
    }
}
