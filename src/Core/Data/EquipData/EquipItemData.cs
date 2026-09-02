namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Tooling.Schema;

    /// <summary>One equipment template from the EquipItems catalog. The catalog is split into a file per
    /// slot, and <see cref="EquipmentPart"/> is the field that says which — a record carries its own
    /// placement.</summary>
    public record EquipItemData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("equipmentPart")][EnumOf(typeof(EquipmentPiece))] public string EquipmentPart { get; init; } = string.Empty;

        /// <summary>Head of the weapon block (type, handedness, damage, crit): read only when the piece is
        /// a weapon, ignored on everything else.</summary>
        [JsonProperty("weaponType")][EnumOf(typeof(WeaponType))] public string WeaponType { get; init; } = string.Empty;

        [JsonProperty("handedness")][EnumOf(typeof(Handedness))] public string Handedness { get; init; } = string.Empty;
        [JsonProperty("damage")] public float Damage { get; init; }
        [JsonProperty("criticalChance")] public float CritChance { get; init; }
        [JsonProperty("criticalDamage")] public float CritDamage { get; init; }
        [JsonProperty("maxStackSize")] public int MaxStackSize { get; init; } = 1;
        [JsonProperty("basePrice")] public int BasePrice { get; init; }
        [JsonProperty("rarity")][EnumOf(typeof(Rarity))] public string Rarity { get; init; } = string.Empty;
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];
        [JsonProperty("updateLevel")] public LevelRangeData UpdateLevel { get; init; }
        [JsonProperty("maxUpdateLevel")] public int MaxUpdateLevel { get; init; }
        [JsonProperty("baseStats")] public List<BaseStatData> BaseStats { get; init; } = [];
        [JsonProperty("implicits")] public List<ItemModifier> Implicits { get; init; } = [];
        [JsonProperty("modifiers")] public List<ItemModifier> Modifiers { get; init; } = [];
        [JsonProperty("grants")] public List<GrantData> Grants { get; init; } = [];
    }
}
