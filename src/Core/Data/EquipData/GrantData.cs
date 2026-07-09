namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record GrantData
    {
        [JsonProperty("kind")] public string Kind { get; init; } = string.Empty;
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("modifiers")] public List<ItemModifier> Modifiers { get; init; } = [];

        /// <summary>Numeric parameters of the granted behavior ("percent": 0.15) — balance lives in data.</summary>
        [JsonProperty("properties")] public Dictionary<string, float> Properties { get; init; } = [];
    }
}
