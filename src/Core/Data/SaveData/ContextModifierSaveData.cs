namespace Core.Data.SaveData
{
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>Rolled context line state: BaseValue only — Value is recomputed from the item's
    /// update multiplier on restore.</summary>
    public class ContextModifierSaveData
    {
        [JsonProperty("parameter")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ContextParameter Parameter { get; init; }

        [JsonProperty("valueType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ModifierValueType ValueType { get; init; }

        [JsonProperty("baseValue")] public float BaseValue { get; init; }
        [JsonProperty("weight")] public float Weight { get; init; }
    }
}
