namespace LastBreath.Script.Items.ItemData
{
    using System.IO;
    using Core.Interfaces.Data;
    using Godot;
    using Godot.Collections;

    public class ItemsMediaProvider : IItemDataProvider<ItemMediaData>
    {
        private const string PathToData = "res://Data/GenericItemMediaData/";
        // Generic data
        private Dictionary<string, ItemMediaData> _mediaData = [];

        public void LoadData()
        {
            var itemData = ResourceLoader.ListDirectory(PathToData);
            // what did i do if i have more then 1 tres within folder???
            foreach (var data in itemData)
            {
                var loadedData = LoadResource(Path.Combine(PathToData, data));
                _mediaData = loadedData.ItemMediaData;
            }
        }

        public ItemMediaData GetItemData(string id)
        {
            if (!_mediaData.TryGetValue(id, out var data))
            {
                // TODO: Log
                return new();
            }
            return data;
        }

        private GenericItemsMediaData LoadResource(string path) => !string.IsNullOrWhiteSpace(path) ? ResourceLoader.Load<GenericItemsMediaData>(path) : new();
    }
}
