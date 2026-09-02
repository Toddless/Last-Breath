namespace Core.Data.EquipData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    public record ItemModifier
    {
        /// <summary>Resolved against two enums in turn — <see cref="EntityParameter"/> first, then
        /// <see cref="ContextParameter"/> — so no single set of members can be offered for it.</summary>
        [JsonProperty("parameter")] public string Parameter { get; init; } = string.Empty;

        /// <summary>An alias out of the parser's own map (flat/add/inc/increase/mult/multi/multiplicative/flag),
        /// which is a wider vocabulary than the enum it lands in.</summary>
        [JsonProperty("modifierType")] public string ModifierType { get; init; } = string.Empty;

        [JsonProperty("scope")][EnumOf(typeof(ModifierScope))] public string Scope { get; init; } = string.Empty;
        [JsonProperty("value")] public ValueRangeData Value { get; init; }
        [JsonProperty("weight")] public float Weight { get; init; }

        /// <summary>Slot family (Prefix/Suffix) for rollable pool entries. Illegal on authored item lines
        /// and on composite parts (only the composite root carries it). Absent = None.</summary>
        [JsonProperty("affix")][EnumOf(typeof(AffixKind))] public string Affix { get; init; } = string.Empty;

        /// <summary>Catalog id of the condition the line only counts under ("while wounded").
        /// Absent = the line always counts. Illegal in two places, both refused at parse with a report
        /// rather than taken and quietly ignored: on a composite part (one bundle is one player-facing line
        /// and carries one condition, on its root) and anywhere inside <see cref="GrantData.Modifiers"/> —
        /// a grant's own lines are minted with it and reach the wearer through it, with no channel of their
        /// own for a predicate.</summary>
        [JsonProperty("condition")][CatalogRef(DataCatalog.Conditions, AllowEmpty = true)] public string? Condition { get; init; }

        /// <summary>Composite entry: one roll grants all parts ("armor AND evade +25%").
        /// When set, the entry's own parameter/type/value are ignored — only weight matters.</summary>
        [JsonProperty("parts")] public List<ItemModifier> Parts { get; init; } = [];

        /// <summary>Mythic-pool entry "the item gains +Min..Max sharpening levels": an OPERATION on the
        /// item, not a stat line — only the ascension gift knows how to apply it. When set, the entry's
        /// parameter/type/value are ignored.</summary>
        [JsonProperty("extraUpgradeLevels")] public LevelRangeData? ExtraUpgradeLevels { get; init; }

        /// <summary>Rollable grant entry: behaviour a stat line cannot express ("ignores the first damage
        /// taken each turn") arrives as a passive/effect instead. When set, the entry's parameter/type/value
        /// are ignored — only weight and affix still apply.</summary>
        [JsonProperty("grant")] public GrantData? Grant { get; init; }
    }
}
