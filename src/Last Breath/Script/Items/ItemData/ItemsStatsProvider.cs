namespace LastBreath.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Core.Enums;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Godot;
    using Newtonsoft.Json;

    // TODO: this class need more attention
    public class ItemsStatsProvider : IItemDataProvider<ItemStats, IEquipItem>
    {
        private readonly Dictionary<EquipmentPart, Dictionary<string, ItemStats>> _itemData = [];
        public void LoadData()
        {
            // For it to work properly, I need to put the JSONs with the data outside the res:// folder.(e.ge %APPDATA%/Godot/app_userdata/Game/Data)
            var userDir = ProjectSettings.GlobalizePath("user://");
            var userDataPath = Path.Combine(userDir, "Data");

            if (Directory.Exists(userDataPath))
            {
                // TODO: Change it later
                // for now recreate each time (i need new data)
                Directory.Delete(userDataPath, true);
                Directory.CreateDirectory(userDataPath);
                CopyDirectory(ProjectSettings.GlobalizePath("res://Data"), userDataPath);
            }
            LoadItemData();
        }

        public ItemStats GetItemData(IEquipItem item)
        {
            if (!_itemData.TryGetValue(item.EquipmentPart, out var dict))
            {
                // TODO: Log
                dict = [];
            }

            if (!dict.TryGetValue(item.Id, out var stats))
            {
                // TODO: Log
                stats = new ItemStats();
            }
            return stats;
        }

        private void LoadItemData()
        {
            foreach (var type in Enum.GetValues<EquipmentPart>())
            {
                var json = ItemsDataPaths.GetDataFromJsonFile(ItemDataFolder.Jsons, $"{type}.json");
                if (string.IsNullOrEmpty(json)) continue;
                var result = DeserializeData(json);
                _itemData.Add(type, result);
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
