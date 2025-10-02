namespace LastBreath.Script.Items.ItemData
{
    using System.Collections.Generic;
    using System.IO;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Godot;
    using Utilities;

    public class ItemsMediaProvider 
    {
        private const string PathToData = "res://Data/GenericItemMediaData/";
        // Generic data
        private Godot.Collections.Dictionary<string, ItemMediaData> _mediaData = [];

        public static IItemDataProvider? Instance => throw new System.NotImplementedException();

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
            if (!_mediaData.TryGetValue(id, out ItemMediaData? data))
            {
                Tracker.TrackNotFound(id, this);
                return new();
            }
            return data;
        }

        private GenericItemsMediaData LoadResource(string path) => !string.IsNullOrWhiteSpace(path) ? ResourceLoader.Load<GenericItemsMediaData>(path) : new();
        public IItem? CopyBaseItem(string id) => throw new System.NotImplementedException();
        public IEnumerable<IItem> GetAllResources() => throw new System.NotImplementedException();
        public IEnumerable<ICraftingRecipe> GetCraftingRecipes() => throw new System.NotImplementedException();
        public List<string> GetItemBaseStats(string id) => throw new System.NotImplementedException();
        public string GetItemDisplayName(string id) => throw new System.NotImplementedException();
        public Texture2D? GetItemIcon(string id) => throw new System.NotImplementedException();
        public int GetItemMaxStackSize(string id) => throw new System.NotImplementedException();
        public ItemStats GetItemStats(string id) => throw new System.NotImplementedException();
        public IReadOnlyList<IMaterialModifier> GetResourceModifiers(string id) => throw new System.NotImplementedException();
    }
}
