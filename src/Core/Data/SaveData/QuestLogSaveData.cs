namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record QuestLogSaveData
    {
        [JsonProperty("quests")] public List<QuestStateSaveData> Quests { get; init; } = [];
    }

    public record QuestStateSaveData
    {
        [JsonProperty("questId")] public string QuestId { get; init; } = string.Empty;
        [JsonProperty("status")] public string Status { get; init; } = string.Empty;
        [JsonProperty("stageIndex")] public int StageIndex { get; init; }
        [JsonProperty("counterBaselines")] public Dictionary<string, int> CounterBaselines { get; init; } = [];
        [JsonProperty("ghostHintShown")] public bool GhostHintShown { get; init; }

        /// <summary>Ids of the one-of-a-kind rewards this quest already paid. A file written before the
        /// field simply carries none, which reads as "nothing has been handed out yet".</summary>
        [JsonProperty("grantedUniqueRewards")] public List<string> GrantedUniqueRewards { get; init; } = [];
        [JsonProperty("acceptedAtMinutes")] public int AcceptedAtMinutes { get; init; }
        [JsonProperty("nextOfferAtMinutes")] public int NextOfferAtMinutes { get; init; }
    }
}
