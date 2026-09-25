namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record RarityUpgradeModifierData : NpcModifierData
    {
        [JsonProperty("multiplier")] public float Multiplier { get; init; }

        /// <summary>Which rarities the multiplier lifts, as their places in the base rarity chances of the
        /// loot configuration. Places and not rarity names: the ladder they index is a list in another
        /// file.</summary>
        [JsonProperty("affectedRarity")] public int[] AffectedRarity { get; init; } = [];

        [JsonProperty("isUnique")] public bool IsUnique { get; init; }
    }
}
