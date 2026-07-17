namespace Core.Data.EquipData
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>Polymorphic "value" field: a plain number (fixed) or a {"min": X, "max": Y} object (rolled range).
    /// The converter never throws on a malformed shape — it yields NaN bounds so the parser can report and skip
    /// the single record instead of failing the whole file (per-entry tolerance).</summary>
    [JsonConverter(typeof(ValueRangeDataConverter))]
    public readonly record struct ValueRangeData(float Min, float Max)
    {
        public bool IsFixed => Min == Max;
        public bool IsValid => !float.IsNaN(Min) && !float.IsNaN(Max) && Min <= Max;
    }

    public class ValueRangeDataConverter : JsonConverter<ValueRangeData>
    {
        public override ValueRangeData ReadJson(JsonReader reader, Type objectType, ValueRangeData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return token.Type switch
            {
                JTokenType.Integer or JTokenType.Float => FromNumber(token.Value<float>()),
                JTokenType.Object => FromObject((JObject)token),
                _ => new ValueRangeData(float.NaN, float.NaN),
            };
        }

        public override void WriteJson(JsonWriter writer, ValueRangeData value, JsonSerializer serializer)
        {
            if (value.IsFixed)
            {
                writer.WriteValue(value.Min);
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("min");
            writer.WriteValue(value.Min);
            writer.WritePropertyName("max");
            writer.WriteValue(value.Max);
            writer.WriteEndObject();
        }

        private static ValueRangeData FromNumber(float value) => new(value, value);

        // A missing bound is NaN, not 0: the record must be reported and skipped, never roll around zero.
        private static ValueRangeData FromObject(JObject obj) => new(ReadBound(obj, "min"), ReadBound(obj, "max"));

        private static float ReadBound(JObject obj, string name) =>
            obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var bound) && bound.Type is JTokenType.Integer or JTokenType.Float
                ? bound.Value<float>()
                : float.NaN;
    }
}
