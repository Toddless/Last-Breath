namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class AbilityBookSaveData
    {
        [JsonProperty("currentStance")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Stance CurrentStance { get; init; }

        /// <summary>Keyed by stance name; tolerates stances added/removed between versions.</summary>
        [JsonProperty("stances")] public Dictionary<string, StanceBookSaveData> Stances { get; init; } = [];

        /// <summary>abilityId → (tier → stable upgrade Id). Restored via Ability.SelectUpgrade.</summary>
        [JsonProperty("upgrades")] public Dictionary<string, Dictionary<int, string>> Upgrades { get; init; } = [];
    }

    public class StanceBookSaveData
    {
        [JsonProperty("learned")] public List<string> Learned { get; init; } = [];

        /// <summary>Ability Id per slot index; null = empty slot.</summary>
        [JsonProperty("slots")] public List<string?> Slots { get; init; } = [];
    }
}
