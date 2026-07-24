namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record EquipItemData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("equipmentPart")] public string EquipmentPart { get; init; } = string.Empty;
        [JsonProperty("weaponType")] public string WeaponType { get; init; } = string.Empty;
        [JsonProperty("handedness")] public string Handedness { get; init; } = string.Empty;
        [JsonProperty("damage")] public float Damage { get; init; }
        [JsonProperty("criticalChance")] public float CritChance { get; init; }
        [JsonProperty("criticalDamage")] public float CritDamage { get; init; }
        [JsonProperty("maxStackSize")] public int MaxStackSize { get; init; } = 1;
        [JsonProperty("basePrice")] public int BasePrice { get; init; }
        [JsonProperty("rarity")] public string Rarity { get; init; } = string.Empty;
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];
        [JsonProperty("updateLevel")] public LevelRangeData UpdateLevel { get; init; }
        [JsonProperty("maxUpdateLevel")] public int MaxUpdateLevel { get; init; }
        [JsonProperty("implicits")] public List<ItemModifier> Implicits { get; init; } = [];
        [JsonProperty("modifiers")] public List<ItemModifier> Modifiers { get; init; } = [];
        [JsonProperty("grants")] public List<GrantData> Grants { get; init; } = [];
    }
}
