namespace PassiveTreeEditor.Source.Io
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// The on-disk shape of a tree, written in the game's own data dialect: Newtonsoft records with
    /// explicit <c>JsonProperty</c> names, camelCase keys, enums as strings. Property order here is
    /// key order in the file — reorder a member and every existing file re-diffs.
    /// </summary>
    public sealed class PassiveTreeDto
    {
        [JsonProperty("version")] public int Version { get; set; } = PassiveTreeFormat.Version;

        [JsonProperty("budget")] public int Budget { get; set; } = 65;

        [JsonProperty("nodes")] public List<PassiveNodeDto> Nodes { get; set; } = [];

        [JsonProperty("edges")] public List<PassiveEdgeDto> Edges { get; set; } = [];
    }

    public sealed class PassiveNodeDto
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;

        [JsonProperty("kind")] public string Kind { get; set; } = string.Empty;

        [JsonProperty("stance", NullValueHandling = NullValueHandling.Ignore)]
        public string? Stance { get; set; }

        [JsonProperty("hybridStance", NullValueHandling = NullValueHandling.Ignore)]
        public string? HybridStance { get; set; }

        [JsonProperty("x")] public float X { get; set; }

        [JsonProperty("y")] public float Y { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string? Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("abilityId", NullValueHandling = NullValueHandling.Ignore)]
        public string? AbilityId { get; set; }

        [JsonProperty("modifiers", NullValueHandling = NullValueHandling.Ignore)]
        public List<ModifierLineDto>? Modifiers { get; set; }
    }

    public sealed class ModifierLineDto
    {
        [JsonProperty("parameter")] public string Parameter { get; set; } = string.Empty;

        [JsonProperty("valueType")] public string ValueType { get; set; } = string.Empty;

        [JsonProperty("value")] public float Value { get; set; }

        [JsonProperty("condition", NullValueHandling = NullValueHandling.Ignore)]
        public string? Condition { get; set; }
    }

    public sealed class PassiveEdgeDto
    {
        [JsonProperty("from")] public string From { get; set; } = string.Empty;

        [JsonProperty("to")] public string To { get; set; } = string.Empty;
    }
}
