namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// World NPC deltas: only the irreversible facts are stored — lying bodies (with their rise
    /// timers), wild risen undead and wild living NPCs nobody re-rolls. Alive NPCs a spawn point
    /// owns stay out: the point restores its own roster.
    /// </summary>
    public class NpcWorldSaveData
    {
        [JsonProperty("bodies")] public List<NpcBodySaveData> Bodies { get; init; } = [];
    }

    public class NpcBodySaveData
    {
        /// <summary>A fresh corpse: the rise timer is running.</summary>
        public const string DefeatedKind = "defeated";

        /// <summary>A defeated undead: lies indefinitely until burned.</summary>
        public const string DormantKind = "dormant";

        /// <summary>A wild undead already risen from a body (alive, roaming).</summary>
        public const string RisenKind = "risen";

        /// <summary>A wild living NPC (a quest's trial target): on its feet, owned by no spawn point,
        /// and the only record that carries its own modifier list.</summary>
        public const string AliveKind = "alive";

        [JsonProperty("kind")] public string Kind { get; init; } = DefeatedKind;

        /// <summary>Whether NO spawn point owned this NPC at save time. False (a version 1 file, or a
        /// body a point counts in its own roster) means the point restores a resident of its own beside
        /// this record, so what comes back here must NOT claim to be nobody's — otherwise the pair the
        /// world used to heal by itself, once the body stands up again, would be written down forever.</summary>
        [JsonProperty("wild")] public bool Wild { get; init; }
        [JsonProperty("npcId")] public string NpcId { get; init; } = string.Empty;
        [JsonProperty("level")] public int Level { get; init; }

        [JsonProperty("rarity")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Rarity Rarity { get; init; }

        [JsonProperty("stance")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Stance Stance { get; init; }

        [JsonProperty("x")] public float X { get; init; }
        [JsonProperty("y")] public float Y { get; init; }

        /// <summary>Defeated only: the rolled rise delay (its length defines the rising's strength).</summary>
        [JsonProperty("resurrectDelay")] public float ResurrectDelay { get; init; }

        /// <summary>Defeated only: seconds already lain.</summary>
        [JsonProperty("elapsed")] public float Elapsed { get; init; }

        /// <summary>Risen only: the parameter bonus the rising granted.</summary>
        [JsonProperty("risingBonus")] public float RisingBonus { get; init; }

        /// <summary>Alive only: the EXACT modifier ids the NPC wears. A trial fought against a
        /// different set is a different trial, so these are restated instead of re-rolled — the
        /// bodies keep re-rolling theirs. Empty is an answer (an authored bare target), not a gap.</summary>
        [JsonProperty("modifiers")] public List<string> Modifiers { get; init; } = [];
    }
}
