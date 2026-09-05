namespace Core.Data.NpcModifiersData
{
    using Newtonsoft.Json;

    public record TierMultiplierData : NpcModifierData
    {
        [JsonProperty("multiplier")] public float Multiplier { get; init; }

        /// <summary>Which tiers the multiplier lifts, as their places in the base tier chances of the loot
        /// configuration. Places and not rarities: the ladder they index is a list in another file.</summary>
        [JsonProperty("affectedTiers")] public int[] AffectedTiers { get; init; } = [];

        [JsonProperty("isUnique")] public bool IsUnique { get; init; }
    }
}
