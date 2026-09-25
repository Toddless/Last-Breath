namespace Core.Data.GameData
{
    using System.Collections.Generic;

    /// <summary>
    /// Where startup game data comes from. Implementations: Godot res:// folders, plain file
    /// system (tests/tooling), potentially a server later. A catalog is a named subfolder of
    /// the source's data root ("Npc", "EquipItems", ...).
    /// </summary>
    public interface IGameDataSource
    {
        /// <summary>Reads every JSON file of a catalog, including nested subfolders.</summary>
        /// <exception cref="System.IO.DirectoryNotFoundException">The catalog does not exist.</exception>
        IReadOnlyList<GameDataFile> ReadCatalog(string catalog);
    }
}
