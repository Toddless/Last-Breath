namespace Core.World.DualGrid
{
    using System.Collections.Generic;

    /// <summary>
    /// Everything a terrain container and its config disagree about, in the order the disagreements were met.
    /// A clean report is the only silent outcome: every other field is a line the container is expected to
    /// shout about, because each one means a terrain quietly does not appear.
    /// </summary>
    public sealed record TerrainRosterReport
    {
        /// <summary>Data layers nothing in the config claims — painted cells with no atlas to draw them with.</summary>
        public IReadOnlyList<string> LayersWithoutEntry { get; init; } = [];

        /// <summary>Config entries no data layer answers to — an atlas with nothing to read.</summary>
        public IReadOnlyList<string> EntriesWithoutLayer { get; init; } = [];

        /// <summary>Layer names carried by more than one layer; each name is listed once.</summary>
        public IReadOnlyList<string> DuplicateLayers { get; init; } = [];

        /// <summary>Keys claimed by more than one entry; each key is listed once, and the first entry wins.</summary>
        public IReadOnlyList<string> DuplicateEntries { get; init; } = [];

        /// <summary>Entries with a blank key: they name no layer at all and can never be matched.</summary>
        public int UnnamedEntries { get; init; }

        public bool IsClean =>
            LayersWithoutEntry.Count == 0
            && EntriesWithoutLayer.Count == 0
            && DuplicateLayers.Count == 0
            && DuplicateEntries.Count == 0
            && UnnamedEntries == 0;
    }
}
