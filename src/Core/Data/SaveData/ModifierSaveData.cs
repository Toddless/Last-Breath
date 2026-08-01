namespace Core.Data.SaveData
{
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>Rolled modifier state: BaseValue only — Value is recomputed from the item's
    /// update multiplier on restore, Source is battle-scoped and never persisted.</summary>
    public class ModifierSaveData
    {
        [JsonProperty("parameter")]
        [JsonConverter(typeof(StringEnumConverter))]
        public EntityParameter Parameter { get; init; }

        [JsonProperty("valueType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ModifierValueType ValueType { get; init; }

        [JsonProperty("scope")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ModifierScope Scope { get; init; }

        [JsonProperty("baseValue")] public float BaseValue { get; init; }
        [JsonProperty("weight")] public float Weight { get; init; } = 1f;

        // Roll provenance stamps (see SimpleModifier): absent for pre-range lines — restore keeps them None/null.
        [JsonProperty("affix", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public AffixKind? Affix { get; init; }

        [JsonProperty("groupId", NullValueHandling = NullValueHandling.Ignore)] public string? GroupId { get; init; }

        /// <summary>Catalog id of the predicate the line was rolled with; absent on a line that always
        /// counts. The predicate itself is rebuilt from the catalog on restore — it holds the state of one
        /// owner and nothing about it is worth storing.</summary>
        [JsonProperty("condition", NullValueHandling = NullValueHandling.Ignore)] public string? Condition { get; init; }

        [JsonProperty("rangeMin", NullValueHandling = NullValueHandling.Ignore)] public float? RangeMin { get; init; }
        [JsonProperty("rangeMax", NullValueHandling = NullValueHandling.Ignore)] public float? RangeMax { get; init; }
    }
}
