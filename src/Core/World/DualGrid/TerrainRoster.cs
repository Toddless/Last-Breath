namespace Core.World.DualGrid
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Matches the data layers of a terrain container against the keys of its config. The whole convention of
    /// the container — one invisible layer per terrain, named after its config entry — lives here as a pure
    /// function, so the mismatch that would otherwise show up as a terrain silently missing from the world is
    /// named before a single tile is painted.
    /// </summary>
    public static class TerrainRoster
    {
        /// <summary>
        /// Names are compared exactly: node names are case-sensitive, so a <c>Tallgrass</c> layer against a
        /// <c>TallGrass</c> entry is a mismatch and is reported as one rather than guessed at.
        /// </summary>
        public static TerrainRosterReport Match(IEnumerable<string> layerNames, IEnumerable<string> entryKeys)
        {
            ArgumentNullException.ThrowIfNull(layerNames);
            ArgumentNullException.ThrowIfNull(entryKeys);

            List<string> layers = FirstSeen(layerNames, out List<string> duplicateLayers, out int _);
            List<string> keys = FirstSeen(entryKeys, out List<string> duplicateEntries, out int unnamedEntries);
            HashSet<string> keySet = [.. keys];
            HashSet<string> layerSet = [.. layers];

            List<string> layersWithoutEntry = [];
            foreach (string layer in layers)
            {
                if (!keySet.Contains(layer)) layersWithoutEntry.Add(layer);
            }

            List<string> entriesWithoutLayer = [];
            foreach (string key in keys)
            {
                if (!layerSet.Contains(key)) entriesWithoutLayer.Add(key);
            }

            return new TerrainRosterReport
            {
                LayersWithoutEntry = layersWithoutEntry,
                EntriesWithoutLayer = entriesWithoutLayer,
                DuplicateLayers = duplicateLayers,
                DuplicateEntries = duplicateEntries,
                UnnamedEntries = unnamedEntries
            };
        }

        /// <summary>Distinct names in the order first met, with the repeats and the blanks split off.</summary>
        private static List<string> FirstSeen(IEnumerable<string> names, out List<string> duplicates, out int blanks)
        {
            List<string> distinct = [];
            HashSet<string> seen = [];
            HashSet<string> repeated = [];
            duplicates = [];
            blanks = 0;

            foreach (string? name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    blanks++;
                    continue;
                }

                if (seen.Add(name)) distinct.Add(name);
                else if (repeated.Add(name)) duplicates.Add(name);
            }

            return distinct;
        }
    }
}
