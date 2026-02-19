namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record TierUpgradeData : NpcModifierData
    {
        [JsonProperty("upgradeMultiplier")] public float UpgradeMultiplier { get; init; }
        [JsonProperty("upgradeBy")] public int UpgradeBy { get; init; }
    }
}
