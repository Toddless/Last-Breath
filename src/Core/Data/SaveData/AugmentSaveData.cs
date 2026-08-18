namespace Core.Data.SaveData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// One augment as a file remembers it. Neither half is the augment on its own, so the two always
    /// travel together: the id names the record every copy shares, and the numbers are what THIS copy
    /// rolled — re-rolling them on load would hand the player a different augment than the one he put
    /// away. Wherever an augment is written down — a socket it sits in, a bag it waits in — this is
    /// the shape it is written in, so one reading answers for all of them.
    /// </summary>
    public class AugmentSaveData
    {
        /// <summary>Stable Id of the augment record the copy was made from.</summary>
        [JsonProperty("augment")] public string Augment { get; init; } = string.Empty;

        /// <summary>
        /// What THIS copy rolled, property by property. Written as the copy carries it: a property the
        /// record has gained since falls back to its declared base when the instance is applied, so a
        /// file is never the reason a number is lost.
        /// </summary>
        [JsonProperty("values")] public Dictionary<string, float> Values { get; init; } = [];

        /// <summary>What this copy rolled on the rarity scale. Absent in files written before copies
        /// carried one: those are drawn once on load, from the band their record declares now.</summary>
        [JsonProperty("rarity")] public Enums.Rarity? Rarity { get; init; }

        /// <summary>Which effect of its record's pool this copy lays. Empty for every copy of a record
        /// that names its own effect, and for files written before pools existed — those are drawn once
        /// on load, out of the pool the record offers now.</summary>
        [JsonProperty("effect")] public string Effect { get; init; } = string.Empty;
    }
}
