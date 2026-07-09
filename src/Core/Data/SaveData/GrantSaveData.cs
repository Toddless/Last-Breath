namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class GrantSaveData
    {
        public const string ModifierKind = "modifier";
        public const string PassiveSkillKind = "passiveSkill";

        [JsonProperty("kind")] public string Kind { get; init; } = ModifierKind;
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("skillId")] public string? SkillId { get; init; }
        [JsonProperty("modifiers")] public List<ModifierSaveData> Modifiers { get; init; } = [];
        [JsonProperty("properties")] public Dictionary<string, float> Properties { get; init; } = [];
    }
}
