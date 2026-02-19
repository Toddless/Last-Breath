namespace Utilities
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Godot;
    using FileAccess = Godot.FileAccess;

    public abstract class DataLoader
    {
        public static async Task LoadDataFromJson(string path, Func<string, Task> loadDataFunc)
        {
            var dir = DirAccess.Open(path);
            dir.ListDirBegin();
            try
            {
                string fileName = dir.GetNext();
                while (!string.IsNullOrWhiteSpace(fileName))
                {
                    string filePath = Path.Combine(path, fileName);
                    if (!filePath.EndsWith(".json")) continue;
                    using var openFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Read) ?? throw new FileLoadException();
                    string jsonContent = openFile.GetAsText() ?? throw new FileLoadException();
                    await loadDataFunc(jsonContent);
                    fileName = dir.GetNext();
                }
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to load equip items", e);
            }
            finally
            {
                dir.ListDirEnd();
            }
        }
    }
}
