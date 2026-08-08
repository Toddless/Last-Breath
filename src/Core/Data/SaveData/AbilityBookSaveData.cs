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
        /// The occupied augment slots, one entry each. A LIST and not a map by socket id: a node
        /// repointed between builds leaves the slot it used to open holding an augment while the slot
        /// it opens now stands beside it, and both belong to the player — keyed by node, the two would
        /// quietly overwrite each other and one of the augments would be gone from the file.
        /// Free slots are not written: which slots exist follows from the passive-tree allocation, and
        /// only what is in them is the file's to remember. Neither is it written whether a slot is
        /// still backed by a node — that follows from the allocation too.
        /// This is the whole of what an ability wears. There is no second list of chosen upgrades any
        /// more: an ability is upgraded by exactly the copies in its sockets, so a file naming both
        /// would be a file able to disagree with itself.
        /// </summary>
        [JsonProperty("sockets")] public List<SocketSaveData> Sockets { get; init; } = [];
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
        /// <summary>The node that opened the slot. Was the key of the map this section used to write;
        /// a plain field since the entries became a list, and read from the key when an older file is
        /// migrated.</summary>
        [JsonProperty("socket")] public string Socket { get; init; } = string.Empty;

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
