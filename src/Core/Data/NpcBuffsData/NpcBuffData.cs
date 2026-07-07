namespace Core.Data.NpcBuffsData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

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
    }

    public record NpcBuffModifierData
    {
        [JsonProperty("parameter")] public string Parameter { get; init; } = string.Empty;
        [JsonProperty("type")] public string Type { get; init; } = "Increase";
        [JsonProperty("value")] public float Value { get; init; }
    }
}
