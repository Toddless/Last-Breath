namespace Core.Data.GameData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Godot;
    using FileAccess = Godot.FileAccess;

    /// <summary>Reads catalogs from a res:// data root through Godot's virtual file system.</summary>
    public class GodotDataSource(string rootPath) : IGameDataSource
    {
        private readonly string _rootPath = rootPath.TrimEnd('/');

        public IReadOnlyList<GameDataFile> ReadCatalog(string catalog)
        {
            var files = new List<GameDataFile>();
            ReadDirectory($"{_rootPath}/{catalog}", files);
            return files;
        }

        private static void ReadDirectory(string path, List<GameDataFile> files)
        {
            using var dir = DirAccess.Open(path)
                            ?? throw new DirectoryNotFoundException($"Data catalog '{path}' does not exist");

            foreach (string subDirectory in dir.GetDirectories())
                ReadDirectory($"{path}/{subDirectory}", files);

            foreach (string fileName in dir.GetFiles())
                if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    files.Add(new GameDataFile(fileName, ReadFile($"{path}/{fileName}")));
        }

        private static string ReadFile(string filePath)
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read)
                             ?? throw new FileLoadException($"Failed to open '{filePath}'");
            return file.GetAsText();
        }
    }
}
