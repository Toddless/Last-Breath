namespace Core.Data.SaveData
{
    using Newtonsoft.Json;

    /// <summary>
    /// Allocation only: the ids the player has taken plus the points still unspent. The modifier
    /// contribution of every node is re-derived from the tree catalog on restore, so a rebalanced
    /// node arrives with its new values instead of the ones that were current when the save was made.
    /// </summary>
    public record PassiveTreeSaveData
    {
        [JsonProperty("allocated")] public string[] Allocated { get; init; } = [];

        /// <summary>Stored rather than derived from mastery: the granted total is what mastery owns,
        /// and re-deriving the remainder here would double-count every point the player already spent.</summary>
        [JsonProperty("availablePoints")] public int AvailablePoints { get; init; }
    }
}
