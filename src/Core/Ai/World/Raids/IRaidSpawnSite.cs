namespace Core.Ai.World.Raids
{
    using System.Collections.Generic;
    using Enums;
    using Godot;

    /// <summary>
    /// A faction's home on the map a raid can march out of — implemented by NpcSpawnPoint.
    /// Raids ground themselves in the world this way: elves raid from their forest, and a
    /// faction with no home on the map simply cannot raid.
    /// </summary>
    public interface IRaidSpawnSite
    {
        /// <summary>Faction of the NPCs living here; null while the data isn't loaded or the point is empty.</summary>
        Fractions? Fraction { get; }

        Vector2 Position { get; }

        IReadOnlyList<string> NpcIds { get; }
    }
}
