namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>Per-NPC personal reputation points (InstanceId → points). Entries whose NPC no
    /// longer exists are harmless leftovers — nobody asks for them.</summary>
    public class PersonalReputationSaveData
    {
        [JsonProperty("points")] public Dictionary<string, int> Points { get; init; } = [];
    }
}
