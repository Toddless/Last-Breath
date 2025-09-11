namespace Crafting.Source
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

    public class ItemDataProvider
    {
        private readonly string _itemDataPath;
        private Dictionary<string, IItem> _itemData = [];
        private Dictionary<string, ItemStats> _itemStatsData = [];

        public static ItemDataProvider? Instance { get; private set; }

        public ItemDataProvider(string itemDataPath)
        {
            // TODO: Path to generic items
            _itemDataPath = itemDataPath;
            Instance = this;
            Logger.LogInfo($"{nameof(ItemDataProvider)} was created.", this);
        }

        public IItem? CopyBaseItem(string id) => TryGetItem(id)?.Copy<IItem>(true);

        public Texture2D? GetItemIcon(string id) => TryGetItem(id)?.Icon;

        public int GetItemMaxStackSize(string id) => TryGetItem(id)?.MaxStackSize ?? 0;

        public string GetItemDisplayName(string id) => TryGetItem(id)?.DisplayName ?? string.Empty;

        public List<string> GetItemBaseStats(string id)
        {
            if (!_itemStatsData.TryGetValue(id, out var data)) return [];
            return ConvertItemStats(data);
        }

        public ItemStats GetItemStats(string id)
        {
            if (!_itemStatsData.TryGetValue(id, out var data)) data = new ItemStats();
            return data;
        }

        public List<IRecipeRequirement> GetRecipeRequirements(string id)
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

        public bool IsItemImplement<T>(string id)
        {
            var item = TryGetItem(id);
            return item != null && item is T;
        }

        public bool IsItemHasTag(string id, string tag) => TryGetItem(id)?.HasTag(tag) ?? false;

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
                            if (dict != null) _itemStatsData = _itemStatsData.Concat(dict).ToDictionary(k => k.Key, k => k.Value);
                        }
                        else
                        {
                            var loadedData = ResourceLoader.Load(Path.Combine(path, file));
                            if (loadedData is IItem item) _itemData.Add(item.Id, item);
                        }
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

        private IItem? TryGetItem(string id)
        {
            if (!_itemData.TryGetValue(id, out var item))
            {
                Logger.LogNotFound($"Item with id: {id}", this);
                return null;
            }
            return item;
        }
    }
}
