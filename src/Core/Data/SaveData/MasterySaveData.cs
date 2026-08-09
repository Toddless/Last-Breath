namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    /// <summary>Base level only: BonusLevel re-accumulates when equipment grants re-attach on load.
    /// The granted tree points have nothing to re-attach to and are written out.</summary>
    public class MasterySaveData
    {
        [JsonProperty("baseLevel")] public int BaseLevel { get; init; }
        [JsonProperty("experience")] public int Experience { get; init; }

        /// <summary>Passive tree points quests handed out, on top of the one per earned level.</summary>
        [JsonProperty("bonusPoints")] public int BonusPoints { get; init; }
    }
}
