namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record ItemModifier
    {
        [JsonProperty("parameter")] public string Parameter { get; init; } = string.Empty;
        [JsonProperty("modifierType")] public string ModifierType { get; init; } = string.Empty;
        [JsonProperty("scope")] public string Scope { get; init; } = string.Empty;
        [JsonProperty("value")] public ValueRangeData Value { get; init; }
        [JsonProperty("weight")] public float Weight { get; init; }

        /// <summary>Slot family (Prefix/Suffix) for rollable pool entries. Illegal on authored item lines
        /// and on composite parts (only the composite root carries it). Absent = None.</summary>
        [JsonProperty("affix")] public string Affix { get; init; } = string.Empty;

        /// <summary>Composite entry: one roll grants all parts ("armor AND evade +25%").
        /// When set, the entry's own parameter/type/value are ignored — only weight matters.</summary>
        [JsonProperty("parts")] public List<ItemModifier> Parts { get; init; } = [];

        /// <summary>Mythic-pool entry "the item gains +Min..Max sharpening levels": an OPERATION on the
        /// item, not a stat line — only the ascension gift knows how to apply it. When set, the entry's
        /// parameter/type/value are ignored.</summary>
        [JsonProperty("extraUpgradeLevels")] public LevelRangeData? ExtraUpgradeLevels { get; init; }
    }
}
