namespace Core.Save
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class SaveSection
    {
        [JsonProperty("version")] public int Version { get; init; }
        [JsonProperty("data")] public JToken Data { get; init; } = JValue.CreateNull();
    }
}
