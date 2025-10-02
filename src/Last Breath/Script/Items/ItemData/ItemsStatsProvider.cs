namespace LastBreath.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Godot;
    using Newtonsoft.Json;
    using Utilities;

    // TODO: this class need more attention
    public class ItemsStatsProvider 
    {
        private const string PathToData = "res://Data/Jsons/";
        private Dictionary<string, ItemStats> _itemStatsData = [];

        public static IItemDataProvider? Instance => throw new NotImplementedException();

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
                Tracker.TrackNotFound(id, this);
                stats = new ItemStats();
            }
            return stats;
        }

        private void LoadItemData()
        {
            using var dir = DirAccess.Open(PathToData);
            if(dir == null)
            {
                Tracker.TrackError($"Cannot open directory. Path: {PathToData}", this);
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
            }catch(Exception ex)
            {
                Tracker.TrackException("Failed to load data", ex, this);
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

        public IItem? CopyBaseItem(string id) => throw new NotImplementedException();
        public IEnumerable<IItem> GetAllResources() => throw new NotImplementedException();
        public IEnumerable<ICraftingRecipe> GetCraftingRecipes() => throw new NotImplementedException();
        public List<string> GetItemBaseStats(string id) => throw new NotImplementedException();
        public string GetItemDisplayName(string id) => throw new NotImplementedException();
        public Texture2D? GetItemIcon(string id) => throw new NotImplementedException();
        public int GetItemMaxStackSize(string id) => throw new NotImplementedException();
        public ItemStats GetItemStats(string id) => throw new NotImplementedException();
        public IReadOnlyList<IMaterialModifier> GetResourceModifiers(string id) => throw new NotImplementedException();
    }
}
