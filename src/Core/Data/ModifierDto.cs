namespace Core.Data
{
    using Newtonsoft.Json;

    public class ModifierDto
    {
        [JsonProperty("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("value")]
        public float Value { get; set; }
    }
}
