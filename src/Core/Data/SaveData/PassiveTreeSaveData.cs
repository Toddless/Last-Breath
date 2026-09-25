namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    /// <summary>
    /// The taken ids and nothing else. The modifier contribution of every node is re-derived from the
    /// tree catalog on restore, so a rebalanced node arrives with its new values instead of the ones
    /// that were current when the save was made. Points are not stored either: the granted total
    /// belongs to mastery and the remainder follows from it and from what the set costs, so a stored
    /// remainder could only contradict them.
    /// </summary>
    public record PassiveTreeSaveData
    {
        [JsonProperty("allocated")] public string[] Allocated { get; init; } = [];
    }
}
