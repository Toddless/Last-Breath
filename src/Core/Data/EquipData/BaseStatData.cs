namespace Core.Data.EquipData
{
    using Enums;
    using Newtonsoft.Json;
    using Tooling.Schema;

    public record BaseStatData
    {
        /// <summary>Base stats live in the entity namespace alone: a pipeline knob has no base value to
        /// carry, and a name that is neither is reported and dropped.</summary>
        [JsonProperty("parameter")][EnumOf(typeof(EntityParameter))] public string Parameter { get; init; } = string.Empty;

        [JsonProperty("value")] public ValueRangeData Value { get; init; }
    }
}
