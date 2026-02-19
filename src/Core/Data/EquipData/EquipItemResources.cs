namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record EquipItemResources
    {
        [JsonProperty("itemId")] public string ItemId { get; init; } = string.Empty;
        [JsonProperty("resources")] public List<EquipItemResource> Resources { get; init; } = [];
    }
}
