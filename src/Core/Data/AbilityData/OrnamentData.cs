namespace Core.Data.AbilityData
{
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// One ornament as its own record declares it: which tier of socket it grants the ability wearing
    /// it. There is no field for the ability — where an ornament currently sits is a fact about the
    /// character and lives in his save, not in the catalog every character reads.
    /// </summary>
    public record OrnamentData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>Which tier the granted socket accepts. The whole of what an ornament does: one more
        /// slot of exactly this tier, beside the ones the tree opens.</summary>
        [JsonProperty("tier")] public int Tier { get; init; }

        /// <summary>Where the ornament stands on the common item scale. Written out rather than left to
        /// the enum's zero (<see cref="Rarity.Legendary"/>): an ornament is one of a handful of named
        /// artefacts in the whole game, and what it is worth is authored rather than inherited.</summary>
        [JsonProperty("rarity")] public Rarity Rarity { get; init; } = Rarity.Unique;
    }

    /// <summary>The ornaments catalog as one file declares it.</summary>
    public record OrnamentDataRoot
    {
        [JsonProperty("ornaments")] public OrnamentData[] Ornaments { get; init; } = [];
    }
}
