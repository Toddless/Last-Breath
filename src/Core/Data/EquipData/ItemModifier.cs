namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record ItemModifier
    {
        [JsonProperty("parameter")] public string Parameter { get; init; } = string.Empty;
        [JsonProperty("modifierType")] public string ModifierType { get; init; } = string.Empty;
        [JsonProperty("scope")] public string Scope { get; init; } = string.Empty;
        [JsonProperty("value")] public float Value { get; init; }
        [JsonProperty("weight")] public float Weight { get; init; }

        /// <summary>Composite entry: one roll grants all parts ("armor AND evade +25%").
        /// When set, the entry's own parameter/type/value are ignored — only weight matters.</summary>
        [JsonProperty("parts")] public List<ItemModifier> Parts { get; init; } = [];
    }
}
