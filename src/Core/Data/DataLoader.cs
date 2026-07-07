namespace Core.Data
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Godot;
    using FileAccess = Godot.FileAccess;

    public abstract class DataLoader
    {
        public static async Task LoadDataFromJson(string path, Func<string, Task> loadDataFunc)
        {
            var dir = DirAccess.Open(path);
            if (dir == null)
            {
                Tracker.TrackError($"Failed to open data directory '{path}'");
                return;
            }

            try
            {
                foreach (string fileName in dir.GetFiles().Where(IsJsonFile))
                    await LoadFile(path, fileName, loadDataFunc);
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Failed to load data from '{path}'", e);
            }
        }

        private static bool IsJsonFile(string fileName) => fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

        private static async Task LoadFile(string path, string fileName, Func<string, Task> loadDataFunc)
        {
            string filePath = Path.Combine(path, fileName);
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read)
                             ?? throw new FileLoadException($"Failed to open '{filePath}'");
            await loadDataFunc(file.GetAsText());
        }
    }
}
