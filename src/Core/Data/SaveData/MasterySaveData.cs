namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    /// <summary>Base level only: BonusLevel re-accumulates when equipment grants re-attach on load.</summary>
    public class MasterySaveData
    {
        [JsonProperty("baseLevel")] public int BaseLevel { get; init; }
        [JsonProperty("experience")] public int Experience { get; init; }
    }
}
