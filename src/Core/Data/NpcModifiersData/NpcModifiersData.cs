namespace Core.Data.NpcModifiersData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public record NpcModifiersData
    {
        [JsonProperty("key")] public string Key { get; init; } = string.Empty;

        /// <summary>How far "unique" reaches for this section — the designer's call, not the code's.
        /// <c>"group"</c> (default): one unique modifier of the whole section at a time, the stronger one
        /// taking the slot (the rarity floors: three at once is three difficulties for one floor's worth).
        /// <c>"id"</c>: only the same modifier twice is refused, different ones live side by side — what the
        /// vault says of the scaling section, "Нпс может иметь несколько разных модификаторов данного типа".</summary>
        [JsonProperty("uniqueScope")] public string UniqueScope { get; init; } = string.Empty;

        [JsonProperty("modifiers")] public List<JToken> Modifiers { get; init; } = [];
    }
}
