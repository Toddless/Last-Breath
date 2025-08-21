namespace Crafting.Source
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Godot;
    using Newtonsoft.Json;

    public class ItemProvider
    {
        private readonly string _pathToItemData;
        private Dictionary<string, IEquipItem> _itemData = [];
        private Dictionary<string, ItemStats> _itemStatsData = [];

        public ItemProvider(string pathToItemData)
        {
            _pathToItemData = pathToItemData;
        }

        public IEquipItem? GetItem(string id)
        {
            if (!_itemData.TryGetValue(id, out var item))
            {
                // TODO: Log
                return null;
            }
            return item.Copy<IEquipItem>(true);
        }

        public IEnumerable<IEquipItem> GetAllItemsWithTag(string tag) => [.. _itemData.Values.Where(x => x.HasTag(tag))];

        public void Initialize()
        {
            var itemData = ResourceLoader.ListDirectory(_pathToItemData);
            foreach (var data in itemData)
            {
                var loadedData = ResourceLoader.Load(Path.Combine(_pathToItemData, data));
                if (loadedData is IEquipItem item) _itemData.Add(item.Id, item);
            }

            using var dir = DirAccess.Open(_pathToItemData);
            if (dir == null)
            {
                // TODO: log
                return;
            }
            dir.ListDirBegin();
            try
            {
                string file;
                while ((file = dir.GetNext()) != "")
                {
                    if (!file.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)) continue;

                    var fullPath = Path.Combine(_pathToItemData, file);
                    var data = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read).GetAsText();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, ItemStats>>(data);
                    if (dict != null) _itemStatsData = _itemStatsData.Concat(dict).ToDictionary(k => k.Key, k => k.Value);
                }
            }
            finally
            {
                dir.ListDirEnd();
            }

        }
    }
}
