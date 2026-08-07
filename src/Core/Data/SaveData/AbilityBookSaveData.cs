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

        /// <summary>
        /// Occupied augment slots: socket Id → what sits in it. The key is the SOCKET, not the tier,
        /// which is what lets one ability hold two augments of the same tier — the two entries differ
        /// by socket even when tier and ability are identical. Free slots are not written: which slots
        /// exist follows from the passive-tree allocation, and only what is in them is the file's to
        /// remember.
        /// This is the whole of what an ability wears. There is no second list of chosen upgrades any
        /// more: an ability is upgraded by exactly the copies in its sockets, so a file naming both
        /// would be a file able to disagree with itself.
        /// </summary>
        [JsonProperty("sockets")] public Dictionary<string, SocketSaveData> Sockets { get; init; } = [];
    }

    /// <summary>
    /// One occupied augment slot: the augment as any file writes one (<see cref="AugmentSaveData"/>),
    /// plus the slot it was chosen for. The slot travels with it because a socket Id outlives the slot
    /// it names: the build reading the file is free to point that node at another ability or another
    /// tier. An entry whose signature no longer matches the slot standing at that id is dropped,
    /// instead of dressing another ability in an augment picked for the one the node used to carry.
    /// </summary>
    public class SocketSaveData : AugmentSaveData
    {
        /// <summary>The ability the slot belonged to when the augment went in.</summary>
        [JsonProperty("ability")] public string Ability { get; init; } = string.Empty;

        /// <summary>The tier the slot accepted when the augment went in.</summary>
        [JsonProperty("tier")] public int Tier { get; init; }
    }

    public class StanceBookSaveData
    {
        [JsonProperty("learned")] public List<string> Learned { get; init; } = [];

        /// <summary>Ability Id per slot index; null = empty slot.</summary>
        [JsonProperty("slots")] public List<string?> Slots { get; init; } = [];
    }
}
