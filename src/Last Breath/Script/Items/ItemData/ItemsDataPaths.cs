namespace Playground.Script.Items.ItemData
{
    using System.IO;
    using Godot;
    using Playground.Script.Enums;

    public abstract class ItemsDataPaths
    {
        private const string DataRoot = "Data";

        public static string GetDataFromJsonFile(ItemDataFolder folder, string fileName)
        {
            var userPath = CreateUserPath(folder, fileName);

            if (File.Exists(userPath))
                return File.ReadAllText(userPath);

            var path = CreatePath(folder, fileName);
            if(string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return File.ReadAllText(path);
        }

        public static string CreatePathToDataFile(ItemDataFolder folder, string fileName)
        {
            //var userPath = CreateUserPath(folder, fileName);
            //if (File.Exists(userPath))
            //    return userPath;

            return CreatePath(folder, fileName);
        }

        public static string GetWeaponDataFromJsonFile(string fileName) => GetDataFromJsonFile(ItemDataFolder.Weapons, fileName);
        public static string GetArmorDataFromJsonFile(string fileName) => GetDataFromJsonFile(ItemDataFolder.Armors, fileName);
        public static string GetJewelleriesDataFromJsonFile(string fileName) => GetDataFromJsonFile(ItemDataFolder.Jewelleries, fileName);
        public static string CreateArmorDataPath(string fileName) => CreatePathToDataFile(ItemDataFolder.Armors, fileName);
        public static string CreateJewelleryDataPath(string fileName) => CreatePathToDataFile(ItemDataFolder.Jewelleries, fileName);
        public static string CreateWeaponDataPath(string fileName) => CreatePathToDataFile(ItemDataFolder.Weapons, fileName);
        private static string CreateUserPath(ItemDataFolder folder, string fileName) => Path.Combine(ProjectSettings.GlobalizePath("user://"), DataRoot, folder.ToString(), fileName);
        private static string CreatePath(ItemDataFolder folder, string fileName)
        {
            var path = Path.Combine(DataRoot, folder.ToString(), fileName);
            return !File.Exists(path) ? string.Empty : path;
        }
    }
}
