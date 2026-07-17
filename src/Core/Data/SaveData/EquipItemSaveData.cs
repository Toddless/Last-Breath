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

        // The generation multiplier (loot difficulty / crafting quality). Nullable so pre-field saves
        // (which also carried a stored reroll pool, now dead) restore with the default of 1.
        [JsonProperty("powerMultiplier", NullValueHandling = NullValueHandling.Ignore)] public float? PowerMultiplier { get; init; }

        // Successful modifier rerolls (drives the growing recraft price). Nullable so legacy saves
        // restore at 0 — their next recraft costs the base price.
        [JsonProperty("recraftCount", NullValueHandling = NullValueHandling.Ignore)] public int? RecraftCount { get; init; }

        // Ascension's flat stat scale (both line channels recompute by it). Nullable so pre-ascension-rework
        // saves — including already-sealed mythics — restore with the neutral 1.
        [JsonProperty("ascensionMultiplier", NullValueHandling = NullValueHandling.Ignore)] public float? AscensionMultiplier { get; init; }

        [JsonProperty("implicits")] public List<ModifierSaveData> Implicits { get; init; } = [];
        [JsonProperty("modifiers")] public List<ModifierSaveData> Modifiers { get; init; } = [];
        [JsonProperty("contextImplicits")] public List<ContextModifierSaveData> ContextImplicits { get; init; } = [];
        [JsonProperty("contextModifiers")] public List<ContextModifierSaveData> ContextModifiers { get; init; } = [];
        // v4: the flat id->amount map became a required/optional split (the item remembers which
        // creation slot each resource came from). Pre-v4 saves restore with empty parts.
        [JsonProperty("usedResources")] public UsedResourcesSaveData UsedResources { get; init; } = new();
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

    /// <summary>The creation resource split: the recipe's mandatory part and the optional additives.</summary>
    public class UsedResourcesSaveData
    {
        [JsonProperty("required")] public Dictionary<string, int> Required { get; init; } = [];
        [JsonProperty("optional")] public Dictionary<string, int> Optional { get; init; } = [];
    }
}
