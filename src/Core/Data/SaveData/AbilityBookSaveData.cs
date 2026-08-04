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

        /// <summary>abilityId → (tier → stable upgrade Id). Restored via Ability.SelectUpgrade, which
        /// holds one choice per tier — a shape that cannot express a second slot of the same tier.
        /// That is what <see cref="Sockets"/> is for; this one carries the upgrade choices until the
        /// choice model itself is retired.</summary>
        [JsonProperty("upgrades")] public Dictionary<string, Dictionary<int, string>> Upgrades { get; init; } = [];

        /// <summary>
        /// Occupied augment slots: socket Id → what sits in it. The key is the SOCKET, not the tier,
        /// which is what lets one ability hold two augments of the same tier — the two entries differ
        /// by socket even when tier and ability are identical. Free slots are not written: which slots
        /// exist follows from the passive-tree allocation, and only what is in them is the file's to
        /// remember.
        /// </summary>
        [JsonProperty("sockets")] public Dictionary<string, SocketSaveData> Sockets { get; init; } = [];
    }

    /// <summary>
    /// One occupied augment slot. The augment is written together with the slot it was chosen for,
    /// because a socket Id outlives the slot it names: the build reading the file is free to point
    /// that node at another ability or another tier. An entry whose signature no longer matches the
    /// slot standing at that id is dropped, instead of dressing another ability in an augment picked
    /// for the one the node used to carry.
    /// </summary>
    public class SocketSaveData
    {
        /// <summary>Stable Id of the augment record in the slot.</summary>
        [JsonProperty("augment")] public string Augment { get; init; } = string.Empty;

        /// <summary>
        /// What THIS copy of the augment rolled, property by property. The id names the record and
        /// every copy of the augment shares it; the numbers are the copy, and re-rolling them on load
        /// would hand the player a different augment than the one he put in the slot.
        /// Written as the copy carries it: a property the record has gained since falls back to its
        /// declared base when the instance is applied, so a file is never the reason a number is lost.
        /// </summary>
        [JsonProperty("values")] public Dictionary<string, float> Values { get; init; } = [];

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
