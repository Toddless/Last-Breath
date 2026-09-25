namespace Core.Data.PlayerStatsData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>
    /// PlayerStats.json: the player's base parameter values, the single source the game and the
    /// passive-tree tool both measure against.
    /// </summary>
    /// <remarks>The unarmed baseline is named as a field rather than the document being a map of
    /// profiles the author invents: one profile is all any reader can reach, and a key nothing reads
    /// is a number the author would tune to no effect.</remarks>
    public record PlayerStatsData
    {
        /// <summary>Json name of the unarmed profile — the character with no weapon and no gear.</summary>
        public const string UnarmedField = "unarmed";

        /// <summary>The base value of every parameter the profile names; one it does not name is worth
        /// zero. Keyed by the parameter itself, which is read strictly — a misspelled key costs its own
        /// line and is reported rather than swallowed.</summary>
        [JsonProperty(UnarmedField)]
        [DictionaryKey(typeof(EntityParameter))]
        public Dictionary<string, float> Unarmed { get; init; } = [];
    }
}
