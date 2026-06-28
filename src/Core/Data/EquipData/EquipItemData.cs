namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record EquipItemData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("equipmentPart")] public string EquipmentPart { get; init; } = string.Empty;
        [JsonProperty("maxStackSize")] public int MaxStackSize { get; init; } = 1;
        [JsonProperty("rarity")] public string Rarity { get; init; } = string.Empty;
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];
        [JsonProperty("attributeType")] public string AttributeType { get; init; } = string.Empty;
        [JsonProperty("effectId")] public string EffectId { get; init; } = string.Empty;
        [JsonProperty("updateLevel")] public int UpdateLevel { get; init; }
        [JsonProperty("maxUpdateLevel")] public int MaxUpdateLevel { get; init; }
        [JsonProperty("implicits")] public List<ItemModifier> Implicits { get; init; } = [];
        [JsonProperty("modifiers")] public List<ItemModifier> Modifiers { get; init; } = [];
    }
}
