namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>ItemEffects.json: the pool of bonus effects a crafted item can roll on creation.</summary>
    public record CraftingEffectsData
    {
        [JsonProperty("effects")] public List<CraftingEffectEntry> Effects { get; init; } = [];
    }

    public record CraftingEffectEntry
    {
        /// <summary>The behaviour handed out, by the id it is written under in its own catalog — which of
        /// the two answers is said by <see cref="Kind"/>. The record has no id besides it: the entry is the
        /// weight and the payload of granting that very passive or effect.</summary>
        [JsonProperty("id")]
        [CatalogRef(DataCatalog.PassiveSkills)]
        [CatalogRef(DataCatalog.Effects)]
        public string Id { get; init; } = string.Empty;

        /// <summary>A <see cref="Core.Enums.GrantKind"/> name, parsed strictly (a typo drops the entry).
        /// Two of the three members send the id to a catalog; a <see cref="GrantKind.Modifier"/> grant names
        /// no record at all — its id is the label its own lines are minted under.</summary>
        [JsonProperty("kind")][EnumOf(typeof(GrantKind))] public string Kind { get; init; } = "Passive";

        [JsonProperty("weight")] public float Weight { get; init; } = 100f;

        /// <summary>Numeric payload for the grant factory — balance lives here, not in code. The keys are
        /// the factory's own: a name the skill does not read hands out nothing, and no catalog holds them.</summary>
        [JsonProperty("properties")] public Dictionary<string, float> Properties { get; init; } = [];
    }
}
