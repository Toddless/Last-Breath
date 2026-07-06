namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record GrantData
    {
        [JsonProperty("kind")] public string Kind { get; init; } = string.Empty;
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("modifiers")] public List<ItemModifier> Modifiers { get; init; } = [];
    }
}
