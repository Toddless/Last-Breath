namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>One reroll-pool descriptor. Kind selects the shape: parameter (entity), context, or composite
    /// (nested parts). Rolled state only — the pool is stored, never re-derived on load.</summary>
    public class ModifierDescriptorSaveData
    {
        public const string ParameterKind = "parameter";
        public const string ContextKind = "context";
        public const string CompositeKind = "composite";

        [JsonProperty("kind")] public string Kind { get; init; } = ParameterKind;

        [JsonProperty("parameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public EntityParameter? Parameter { get; init; }

        [JsonProperty("contextParameter", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ContextParameter? ContextParameter { get; init; }

        [JsonProperty("valueType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ModifierValueType? ValueType { get; init; }

        [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ModifierScope? Scope { get; init; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)] public float? Value { get; init; }
        [JsonProperty("weight")] public float Weight { get; init; }
        [JsonProperty("parts", NullValueHandling = NullValueHandling.Ignore)] public List<ModifierDescriptorSaveData>? Parts { get; init; }
    }
}
