namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>ItemEffects.json: the pool of bonus effects a crafted item can roll on creation.</summary>
    public record CraftingEffectsData
    {
        [JsonProperty("effects")] public List<CraftingEffectEntry> Effects { get; init; } = [];
    }

    public record CraftingEffectEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>A <see cref="Core.Enums.GrantKind"/> name, parsed strictly (a typo drops the entry).</summary>
        [JsonProperty("kind")] public string Kind { get; init; } = "Passive";

        [JsonProperty("weight")] public float Weight { get; init; } = 100f;

        /// <summary>Numeric payload for the grant factory — balance lives here, not in code.</summary>
        [JsonProperty("properties")] public Dictionary<string, float> Properties { get; init; } = [];
    }
}
