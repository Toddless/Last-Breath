namespace LastBreath.World
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>
    /// The terrain table of one location: which data layers a <see cref="TerrainRoot"/> knows about and what
    /// each of them is painted with. A location swaps its whole look by pointing at another table.
    /// </summary>
    [GlobalClass]
    public partial class TerrainRootConfig : Resource
    {
        [Export] public Godot.Collections.Array<TerrainEntry> Entries { get; set; } = [];

        /// <summary>
        /// Layer names the table claims, in table order. An empty slot left in the array answers with a blank
        /// name so the roster counts it as an unnamed entry instead of vanishing from the check.
        /// </summary>
        public List<string> Keys()
        {
            List<string> keys = new(Entries.Count);
            foreach (TerrainEntry? entry in Entries) keys.Add(entry?.Layer ?? "");

            return keys;
        }

        /// <summary>Entries by layer name, first claim winning — the duplicates are named by the roster check.</summary>
        public Dictionary<string, TerrainEntry> ByLayer()
        {
            Dictionary<string, TerrainEntry> byLayer = [];
            foreach (TerrainEntry? entry in Entries)
            {
                if (entry is null || string.IsNullOrWhiteSpace(entry.Layer)) continue;

                byLayer.TryAdd(entry.Layer, entry);
            }

            return byLayer;
        }
    }
}
