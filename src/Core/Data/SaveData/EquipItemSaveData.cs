namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Full round-trip of a procedurally rolled item: rolled RESULTS are stored, never seeds —
    /// reloading must not reroll the item.
    /// </summary>
    public class EquipItemSaveData
    {
        public const string EquipKind = "equip";
        public const string WeaponKind = "weapon";

        [JsonProperty("kind")] public string Kind { get; init; } = EquipKind;
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        [JsonProperty("piece")]
        [JsonConverter(typeof(StringEnumConverter))]
        public EquipmentPiece Piece { get; init; }

        [JsonProperty("tags")] public string[] Tags { get; init; } = [];

        [JsonProperty("rarity")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; init; }

        [JsonProperty("updateLevel")] public int UpdateLevel { get; init; }
        [JsonProperty("maxUpdateLevel")] public int MaxUpdateLevel { get; init; }
        [JsonProperty("isSealed")] public bool IsSealed { get; init; }
        [JsonProperty("itemEffect")] public string ItemEffect { get; init; } = string.Empty;

        [JsonProperty("implicits")] public List<ModifierSaveData> Implicits { get; init; } = [];
        [JsonProperty("modifiers")] public List<ModifierSaveData> Modifiers { get; init; } = [];
        [JsonProperty("contextImplicits")] public List<ContextModifierSaveData> ContextImplicits { get; init; } = [];
        [JsonProperty("contextModifiers")] public List<ContextModifierSaveData> ContextModifiers { get; init; } = [];
        [JsonProperty("modifiersPool")] public List<ModifierSaveData> ModifiersPool { get; init; } = [];
        [JsonProperty("usedResources")] public Dictionary<string, int> UsedResources { get; init; } = [];
        [JsonProperty("grants")] public List<GrantSaveData> Grants { get; init; } = [];

        // Weapon-only block (Kind == WeaponKind)
        [JsonProperty("weaponType", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public WeaponType? WeaponType { get; init; }

        [JsonProperty("handedness", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Handedness? Handedness { get; init; }

        [JsonProperty("baseDamage", NullValueHandling = NullValueHandling.Ignore)] public float? BaseDamage { get; init; }
        [JsonProperty("criticalChance", NullValueHandling = NullValueHandling.Ignore)] public float? CriticalChance { get; init; }
        [JsonProperty("criticalDamage", NullValueHandling = NullValueHandling.Ignore)] public float? CriticalDamage { get; init; }
    }
}
