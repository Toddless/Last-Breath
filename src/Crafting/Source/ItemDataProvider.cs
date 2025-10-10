namespace Crafting.Source
{
    using Godot;
    using System;
    using System.IO;
    using Utilities;
    using System.Linq;
    using Newtonsoft.Json;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Modifiers;

    public class ItemDataProvider
    {
        private readonly string _itemDataPath;
        private Dictionary<string, IItem> _itemData = [];
        private Dictionary<string, List<IModifier>> _itemBaseStatsData = [];

        public static ItemDataProvider? Instance { get; private set; }

        public ItemDataProvider(string itemDataPath)
        {
            // TODO: Path to generic items
            _itemDataPath = itemDataPath;
            Instance = this;
        }

        public IItem? CopyBaseItem(string id) => TryGetItem(id, out var item)?.Copy<IItem>();

        public Texture2D? GetItemIcon(string id) => TryGetItem(id, out var item)?.Icon;

        public string GetItemDisplayName(string id) => TryGetItem(id, out var item)?.DisplayName ?? string.Empty;

        public List<IModifier> GetItemBaseStats(string id)
        {
            if (!_itemBaseStatsData.TryGetValue(id, out var data)) return [];
            return [.. data];
        }

        public List<IResourceRequirement> GetRecipeRequirements(string id)
        {
            TryGetItem(id, out var item);
            if (item is not ICraftingRecipe recipe) return [];
            return recipe.MainResource;
        }

        public string GetRecipeResultItemId(string recipeId)
        {
            TryGetItem(recipeId, out var item);
            if (item is not ICraftingRecipe recipe) return string.Empty;
            return recipe.ResultItemId;
        }

        public bool IsItemImplement<T>(string id)
        {
            TryGetItem(id, out var item);
            return item != null && item is T;
        }

        public bool IsItemHasTag(string id, string tag) => TryGetItem(id, out var item)?.HasTag(tag) ?? false;

        public IReadOnlyList<IMaterialModifier> GetResourceModifiers(string id)
        {
            if (!_itemData.TryGetValue(id, out var res)) return [];
            if (res is not ICraftingResource crafting) return [];
            return crafting.MaterialType?.Modifiers ?? [];
        }

        public IEnumerable<IItem> GetAllResources() => [.. _itemData.Values.Where(x => x is IResource)];

        public IEnumerable<ICraftingRecipe> GetCraftingRecipes() => [.. _itemData.Values.Where(x => x is ICraftingRecipe).Cast<ICraftingRecipe>()];

        public void LoadData()
        {
            var itemData = ResourceLoader.ListDirectory(_itemDataPath);
            foreach (var data in itemData)
            {
                var path = Path.Combine(_itemDataPath, data);
                using var dir = DirAccess.Open(path);
                if (dir == null)
                {
                    continue;
                }
                dir.ListDirBegin();
                try
                {
                    string file;
                    while ((file = dir.GetNext()) != "")
                    {
                        if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            var fullPath = Path.Combine(path, file);
                            var itemStats = Godot.FileAccess.Open(fullPath, Godot.FileAccess.ModeFlags.Read).GetAsText();
                            var dict = JsonConvert.DeserializeObject<Dictionary<string, ItemStats>>(itemStats);
                            if(dict != null)
                            {
                                foreach (var item in dict)
                                {
                                    var mods = ModifiersCreator.ItemStatsToModifiers(item.Value);
                                    _itemBaseStatsData[item.Key] = mods;
                                }
                            }
                        }
                        else if (file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                        {
                            var loadedData = ResourceLoader.Load(Path.Combine(path, file));
                            if (loadedData is IItem item) _itemData.Add(item.Id, item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tracker.TrackException("Data loading failed.", ex, this);
                }
                finally
                {
                    dir.ListDirEnd();
                }

            }
        }

        private IItem? TryGetItem(string id, out IItem? item)
        {
            if (!_itemData.TryGetValue(id, out var data))
            {
                Tracker.TrackNotFound($"Item with id: {id}", this);
                return item = null;
            }
            return item = data;
        }
    }
}
