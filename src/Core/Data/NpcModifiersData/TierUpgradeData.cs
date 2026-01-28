namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record TierUpgradeData : NpcModifierData
    {
        [JsonProperty("tierUpgradeChance")] public float TierUpgradeChance { get; init; }
        [JsonProperty("upgradeBy")] public int UpgradeBy { get; init; }
    }
}
