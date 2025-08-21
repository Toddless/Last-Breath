namespace LastBreath.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Interfaces.Data;
    using Godot;
    using Newtonsoft.Json;

    // TODO: this class need more attention
    public class ItemsStatsProvider : IItemDataProvider<ItemStats>
    {
        private const string PathToData = "res://Data/Jsons/";
        private Dictionary<string, ItemStats> _itemStatsData = [];

        public void LoadData()
        {
            // For it to work properly, I need to put the JSONs with the data outside the res:// folder.(e.ge %APPDATA%/Godot/app_userdata/Game/Data)
            var userDir = ProjectSettings.GlobalizePath("user://");
            var userDataPath = Path.Combine(userDir, "Data");

            if (!Directory.Exists(userDataPath))
            {
                // TODO: Change it later
                // for now recreate each time (i need new data)
                Directory.CreateDirectory(userDataPath);
                CopyDirectory(ProjectSettings.GlobalizePath("res://Data"), userDataPath);
            }
            LoadItemData();
        }

        public ItemStats GetItemData(string id)
        {
            if (!_itemStatsData.TryGetValue(id, out var stats))
            {
                // TODO: Log
                stats = new ItemStats();
            }
            return stats;
        }

        private void LoadItemData()
        {
            using var dir = DirAccess.Open(PathToData);
            if(dir == null)
            {
                // TODO : log
                return;
            }
            dir.ListDirBegin();
            try
            {
                string file;
                while((file = dir.GetNext())!= "")
                {
                    if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                    var fullPath = Path.Combine(PathToData, file);
                    var data = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read).GetAsText();
                    var dict = DeserializeData(data);
                    _itemStatsData = _itemStatsData.Concat(dict).ToDictionary(x => x.Key, x => x.Value);
                }
            }
            finally
            {
                dir.ListDirEnd();
            }
        }

        private static Dictionary<string, ItemStats> DeserializeData(string json) => JsonConvert.DeserializeObject<Dictionary<string, ItemStats>>(json) ?? [];

        private void CopyDirectory(string srcDir, string userDataPath)
        {
            foreach (var dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(srcDir, userDataPath));

            foreach (var file in Directory.GetFiles(srcDir, "*.json", SearchOption.AllDirectories))
            {
                var dest = file.Replace(srcDir, userDataPath);
                File.Copy(file, dest, overwrite: true);
            }
        }
    }
}
