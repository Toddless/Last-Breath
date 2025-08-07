namespace LastBreath.Script.Items.ItemData
{
    using System.IO;
    using Core.Enums;
    using Godot;

    public abstract class ItemsDataPaths
    {
        private const string DataRoot = "Data";

        public static string GetDataFromJsonFile(ItemDataFolder folder, string fileName)
        {
            var userPath = CreateUserPath(folder, fileName);

            if (File.Exists(userPath))
                return File.ReadAllText(userPath);

            var path = CreatePath(folder, fileName);
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return File.ReadAllText(path);
        }

        public static string CreatePath(ItemDataFolder folder, string fileName)
        {
            var path = Path.Combine(DataRoot, folder.ToString(), fileName);
            return !File.Exists(path) ? string.Empty : path;
        }

        private static string CreateUserPath(ItemDataFolder folder, string fileName) => Path.Combine(ProjectSettings.GlobalizePath("user://"), DataRoot, folder.ToString(), fileName);
    }
}
