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
    }
}
