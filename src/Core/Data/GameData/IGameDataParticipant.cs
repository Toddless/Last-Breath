namespace Core.Data.GameData
{
    using System.Collections.Generic;

    /// <summary>
    /// A consumer of startup game data — the read-side mirror of ISaveParticipant. A provider
    /// declares which catalogs it eats and applies each file; <see cref="GameDataService"/>
    /// orchestrates the one-time load at bootstrap, so constructors stay free of IO.
    /// </summary>
    public interface IGameDataParticipant
    {
        IReadOnlyList<string> Catalogs { get; }

        /// <summary>Parses one file of one of the declared catalogs. Throw on bad data — the service reports and skips the file.</summary>
        void Apply(string catalog, GameDataFile file);
    }
}
