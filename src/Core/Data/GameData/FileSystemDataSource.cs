namespace Core.Data.GameData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>Reads catalogs from a plain directory — tests and tooling, no Godot involved.</summary>
    public class FileSystemDataSource(string rootPath) : IGameDataSource
    {
        public IReadOnlyList<GameDataFile> ReadCatalog(string catalog)
        {
            string path = Path.Combine(rootPath, catalog);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Data catalog '{path}' does not exist");

            return Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories)
                .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
                .Select(filePath => new GameDataFile(Path.GetFileName(filePath), File.ReadAllText(filePath)))
                .ToList();
        }
    }
}
