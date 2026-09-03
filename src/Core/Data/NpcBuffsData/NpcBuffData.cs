namespace Core.Data.NpcBuffsData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>NpcBuffs.json: the parameter side of NPC modifiers, keyed by NpcBuffId.</summary>
    public record NpcBuffsData
    {
        [JsonProperty("buffs")] public List<NpcBuffData> Buffs { get; init; } = [];
    }

    /// <summary>One buff: the parameter modifiers an NPC gains when a modifier with this NpcBuffId attaches.</summary>
    public record NpcBuffData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("modifiers")] public List<NpcBuffModifierData> Modifiers { get; init; } = [];

        /// <summary>What the bearer gains that is not a number: the "Палач fights WITH Execute" side of a
        /// modifier. Same shape and the same <c>IGrantFactory</c> as an item grant, so a granted passive
        /// reaches the NPC through the channel gear already uses — and the property keys here are the
        /// skill factory's keys, not free-form labels.</summary>
        [JsonProperty("grants")] public List<NpcBuffGrantData> Grants { get; init; } = [];
    }

    public record NpcBuffModifierData
    {
        [JsonProperty("parameter")][EnumOf(typeof(EntityParameter))] public string Parameter { get; init; } = string.Empty;
        [JsonProperty("type")][EnumOf(typeof(ModifierValueType))] public string Type { get; init; } = "Increase";
        [JsonProperty("value")] public float Value { get; init; }
    }

    /// <summary>One grant line of a buff. <see cref="Kind"/> is a <c>GrantKind</c> name (Passive today);
    /// <see cref="Id"/> is the skill id; <see cref="Properties"/> are the numeric parameters that skill's
    /// factory declares — a missing key is a refusal at mint, not a silent no-op.</summary>
    public record NpcBuffGrantData
    {
        [JsonProperty("kind")][EnumOf(typeof(GrantKind))] public string Kind { get; init; } = "Passive";

        /// <summary>The behaviour handed over, answered from whichever catalog declares it — the kind
        /// beside it says which.</summary>
        [CatalogRef(DataCatalog.PassiveSkills)]
        [CatalogRef(DataCatalog.Effects)]
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("properties")] public Dictionary<string, float> Properties { get; init; } = [];
    }
}
