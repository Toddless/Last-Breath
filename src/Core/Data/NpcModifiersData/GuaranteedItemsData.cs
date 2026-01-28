namespace Core.Data.NpcModifiersData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record GuaranteedItemsData : NpcModifierData
    {
        [JsonProperty("items")] public List<string> Items { get; init; } = [];
    }
}
