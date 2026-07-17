namespace Core.Data.EquipData
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>Polymorphic "updateLevel" field: a plain number (exact starting level) or a
    /// {"min": X, "max": Y} object — the range spec the item minter will roll (next block); until then
    /// templates apply Min. A malformed shape is flagged invalid so the parser skips the whole item.</summary>
    [JsonConverter(typeof(LevelRangeDataConverter))]
    public readonly record struct LevelRangeData(int Min, int Max)
    {
        public bool IsFixed => Min == Max;
        public bool IsMalformed { get; init; }
    }

    public class LevelRangeDataConverter : JsonConverter<LevelRangeData>
    {
        public override LevelRangeData ReadJson(JsonReader reader, Type objectType, LevelRangeData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return token.Type switch
            {
                JTokenType.Integer => FromNumber(token.Value<int>()),
                JTokenType.Object => FromObject((JObject)token),
                _ => new LevelRangeData { IsMalformed = true },
            };
        }

        public override void WriteJson(JsonWriter writer, LevelRangeData value, JsonSerializer serializer)
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

        private static LevelRangeData FromNumber(int value) => new(value, value);

        private static LevelRangeData FromObject(JObject obj) =>
            TryReadBound(obj, "min", out int min) && TryReadBound(obj, "max", out int max)
                ? new LevelRangeData(min, max)
                : new LevelRangeData { IsMalformed = true };

        private static bool TryReadBound(JObject obj, string name, out int value)
        {
            value = 0;
            if (!obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var bound) || bound.Type != JTokenType.Integer) return false;
            value = bound.Value<int>();
            return true;
        }
    }
}
