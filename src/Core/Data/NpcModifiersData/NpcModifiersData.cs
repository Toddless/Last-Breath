namespace Core.Data.NpcModifiersData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public record NpcModifiersData
    {
        [JsonProperty("key")] public string Key { get; init; } = string.Empty;
        [JsonProperty("modifiers")] public List<JToken> Modifiers { get; init; } = [];
    }
}
