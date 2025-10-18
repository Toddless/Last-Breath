namespace Crafting.Source
{
    using Godot;
    using System;
    using System.IO;
    using Utilities;
    using System.Linq;
    using Core.Modifiers;
    using Newtonsoft.Json;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class ItemDataProvider : IItemDataProvider
    {
        private readonly string _itemDataPath;
        private Dictionary<string, IItem> _itemData = [];
        private Dictionary<string, List<IModifier>> _itemBaseStatsData = [];

        public ItemDataProvider(string itemDataPath)
        {
            // TODO: Path to generic items
            _itemDataPath = itemDataPath;
        }

        public IItem CopyBaseItem(string id) => TryGetItem(id)?.Copy<IItem>() ?? throw new ArgumentNullException($"Item not found: {id}");

        public Texture2D? GetItemIcon(string id) => TryGetItem(id)?.Icon;

        public List<IModifier> GetItemBaseStats(string id)
        {
            if (!_itemBaseStatsData.TryGetValue(id, out var data)) return [];
            return [.. data];
        }

        public List<IResourceRequirement> GetRecipeRequirements(string id)
        {
            var item = TryGetItem(id);
            if (item is not ICraftingRecipe recipe) return [];
            return recipe.MainResource;
        }

        public string GetRecipeResultItemId(string recipeId)
        {
            var item = TryGetItem(recipeId);
            if (item is not ICraftingRecipe recipe) return string.Empty;
            return recipe.ResultItemId;
        }

        public IReadOnlyList<IMaterialModifier> GetResourceModifiers(string id)
        {
            if (!_itemData.TryGetValue(id, out var res)) return [];
            if (res is not ICraftingResource crafting) return [];
            return crafting.MaterialType?.Modifiers ?? [];
        }

        public bool IsItemHasTag(string id, string tag) => TryGetItem(id)?.HasTag(tag) ?? false;

        public IEnumerable<IItem> GetAllResources() => [.. _itemData.Values.Where(x => x is IResource)];

        public IEnumerable<ICraftingRecipe> GetCraftingRecipes() => [.. _itemData.Values.Where(x => x is ICraftingRecipe).Cast<ICraftingRecipe>()];

        public void LoadData()
        {
            try
            {
                LoadDataFromDirectory(_itemDataPath);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Data loading failed.", ex, this);
            }
        }

        private void LoadDataFromDirectory(string directoryPath)
        {
            var itemData = ResourceLoader.ListDirectory(directoryPath);
            foreach (var item in itemData)
            {
                var path = Path.Combine(directoryPath, item);

                if (ShouldSkipItem(path))
                    continue;

                if (IsDirectory(path))
                {
                    LoadDataFromDirectory(path);
                }
                else
                {
                    ProcessFile(path);
                }
            }
        }

        private void ProcessFile(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                switch (extension)
                {
                    case ".json":
                        ProcessJsonFile(filePath);
                        break;
                    case ".tres":
                        ProcessTresFile(filePath);
                        break;
                }
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to process file: {filePath}", ex, this);
            }
        }

        private void ProcessJsonFile(string filePath)
        {
            using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read) ?? throw new FileLoadException();

            var jsonContent = file.GetAsText() ?? throw new FileNotFoundException();

            var dict = JsonConvert.DeserializeObject<Dictionary<string, ItemStats>>(jsonContent) ?? throw new FileNotFoundException();

            foreach (var item in dict)
            {
                var mod = ModifiersCreator.ItemStatsToModifiers(item.Value);
                _itemBaseStatsData[item.Key] = mod;
            }
        }

        private void ProcessTresFile(string filePath)
        {
            var loaded = ResourceLoader.Load(filePath);
            if (loaded is IItem item)
                _itemData.Add(item.Id, item);
        }

        private bool IsDirectory(string path)
        {
            using var dir = DirAccess.Open(path);
            return dir != null;
        }

        private bool ShouldSkipItem(string itemName) => itemName.StartsWith(".") || itemName.Equals("__MACOSX", StringComparison.OrdinalIgnoreCase);

        private IItem? TryGetItem(string id)
        {
            if (!_itemData.TryGetValue(id, out var data))
            {
                Tracker.TrackNotFound($"Item with id: {id}", this);
                return null;
            }
            return data;
        }
    }
}
