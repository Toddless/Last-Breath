namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// World NPC deltas: only the irreversible facts are stored — lying bodies (with their rise
    /// timers) and wild risen undead. Regular alive NPCs re-roll from spawn points on load.
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

        [JsonProperty("kind")] public string Kind { get; init; } = DefeatedKind;
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
    }
}
