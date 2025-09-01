namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Godot;
    using Newtonsoft.Json;
    using Utilities;

    public class EquipItemDataProvider
    {
        private readonly string _pathToEquipItemData;
        private Dictionary<string, IEquipItem> _itemData = [];
        private Dictionary<string, ItemStats> _itemStatsData = [];

        public static EquipItemDataProvider? Instance { get; private set; }

        public EquipItemDataProvider(string pathToItemData)
        {
            // TODO: Path to generic items
            _pathToEquipItemData = pathToItemData;
            Instance = this;
        }

        public IEquipItem? GetItem(string id)
        {
            if (!_itemData.TryGetValue(id, out var item))
            {
                Logger.LogNotFound(id, this);
                return null;
            }
            return item.Copy<IEquipItem>(true);
        }

        public Texture2D? GetItemImage(string id)
        {
            if (!_itemData.TryGetValue(id, out var item))
            {
                Logger.LogNotFound(id, this);
                return null;
            }

            return item.FullImage ?? item.Icon;
        }

        public List<string> GetItemBaseStats(string id)
        {
            if (!_itemStatsData.TryGetValue(id, out var item))
            {
                Logger.LogNotFound(id, this);
                return [];
            }
            return ConvertItemStats(item);
        }

        public ItemStats GetItemStats(string id)
        {
            if (!_itemStatsData.TryGetValue(id, out var data))
            {
                Logger.LogNotFound(id, this);
                data = new ItemStats();
            }
            return data;
        }

        public IEnumerable<IEquipItem> GetAllItemsWithTag(string tag) => [.. _itemData.Values.Where(x => x.HasTag(tag))];

        public void LoadData()
        {
            var itemData = ResourceLoader.ListDirectory(_pathToEquipItemData);
            foreach (var data in itemData)
            {
                var loadedData = ResourceLoader.Load(Path.Combine(_pathToEquipItemData, data));
                if (loadedData is IEquipItem item) _itemData.Add(item.Id, item);
            }

            using var dir = DirAccess.Open(_pathToEquipItemData);
            if (dir == null)
            {
                Logger.LogNull(nameof(dir), this);
                return;
            }
            dir.ListDirBegin();
            try
            {
                string file;
                while ((file = dir.GetNext()) != "")
                {
                    if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

                    var fullPath = Path.Combine(_pathToEquipItemData, file);
                    var data = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read).GetAsText();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, ItemStats>>(data);
                    if (dict != null) _itemStatsData = _itemStatsData.Concat(dict).ToDictionary(k => k.Key, k => k.Value);
                }

            }
            catch (Exception ex)
            {
                Logger.LogException("Data loading failed.", ex, this);
            }
            finally
            {
                dir.ListDirEnd();
            }
        }

        // should not be there
        private List<string> ConvertItemStats(ItemStats stats)
        {
            var properties = stats.GetType().GetProperties();
            List<string> dict = [];
            foreach (var prop in properties)
            {
                var value = Convert.ToSingle(prop.GetValue(stats));
                if (value <= 0) continue;
                dict.Add(Lokalizator.FormatItemStats(prop.Name, value));
            }

            return dict;
        }
    }
}
