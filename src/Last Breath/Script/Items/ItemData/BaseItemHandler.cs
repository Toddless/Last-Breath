namespace Playground.Script.Items.ItemData
{
    using System.IO;

    public abstract class BaseItemHandler
    {
        protected const string DataRoot = "Data";
        protected const string Weapons = nameof(Weapons);
        protected const string Armors = nameof(Armors);
        protected const string Jewelleries = nameof(Jewelleries);

        protected static string GetJson(string folder, string jsonName)
        {
            var path = Path.Combine(DataRoot, folder, jsonName);
            if (!File.Exists(path))
            {
                return string.Empty;
            }
            return File.ReadAllText(path);
        }
    }
}
