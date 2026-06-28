namespace Crafting.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Godot;
    using Utilities;

    internal class ItemDataProvider : IItemDataProvider
    {
        private const string itemDataPath = "res://Internal/Data/";
        private readonly Dictionary<string, IItem> _itemData = [];
        private readonly Dictionary<string, List<IModifier>> _equipItemModifierPools = [];
        private readonly IDataParser _dataParser;

        public ItemDataProvider(IItemGameDataFactory factory)
        {
            _dataParser = new DataParser(factory);
            LoadData();
        }

        public IItem CopyItem(string id) => TryGetItem(id)?.Copy<IItem>() ?? throw new ArgumentNullException($"Item not found: {id}");

        public Texture2D? GetItemIcon(string id) => TryGetItem(id)?.Icon;

        public List<IModifier> GetEquipItemModifierPool(string id)
        {
            if (!_equipItemModifierPools.TryGetValue(id, out var data)) return [];
            return [.. data];
        }

        public Dictionary<string, int> GetEquipItemResources(string itemId) => throw new NotImplementedException();
        public List<IModifier> GetEquipItemBaseModifierPool(string id) => throw new NotImplementedException();

        public List<IRequirement> GetRecipeRequirements(string id)
        {
            var item = TryGetItem(id);
            return item is not ICraftingRecipe recipe ? [] : recipe.Requirements;
        }

        public string GetRecipeResultItemId(string recipeId)
        {
            var item = TryGetItem(recipeId);
            return item is not ICraftingRecipe recipe ? string.Empty : recipe.ResultItemId;
        }

        public IReadOnlyList<IModifier> GetResourceModifiers(string id)
        {
            if (!_itemData.TryGetValue(id, out var res) || res is not ICraftingResource crafting) return [];
            return crafting.Material?.Modifiers ?? [];
        }

        public ICraftingRecipe GetRecipe(string recipeId)
        {
            var item = TryGetItem(recipeId);
            if (item is not ICraftingRecipe recipe) throw new ArgumentNullException($"Recipe not found: {recipeId}");
            return recipe;
        }

        public bool IsItemHasTag(string id, string tag) => TryGetItem(id)?.HasTag(tag) ?? false;

        public IEnumerable<IItem> GetAllResources() => [.. _itemData.Values.Where(x => x is IResource)];

        public IEnumerable<ICraftingRecipe> GetCraftingRecipes() => [.. _itemData.Values.Where(x => x is ICraftingRecipe).Cast<ICraftingRecipe>()];

        public async void LoadData()
        {
            try
            {
                await LoadDataFromDirectory(itemDataPath);
            }
            catch (Exception ex)
            {
                GD.Print($"Failed to load item data: {ex.Message}\n{ex.StackTrace}");
                Tracker.TrackException("Data loading failed.", ex, this);
            }
        }

        private async Task LoadDataFromDirectory(string dataPath)
        {
            using var dir = DirAccess.Open(dataPath);
            if (dir == null) return;

            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != string.Empty)
            {
                string filePath = System.IO.Path.Combine(dataPath, fileName);
                if (dir.CurrentIsDir())
                    await LoadDataFromDirectory(filePath);
                else
                {
                    if (!filePath.EndsWith(".json")) continue;
                    using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read) ?? throw new System.IO.FileLoadException();
                    string jsonContent = file.GetAsText() ?? throw new System.IO.FileNotFoundException();

                    List<IItem> data = dataPath switch
                    {
                        _ when dataPath.EndsWith("EquipItems") => await _dataParser.ParseEquipItems(jsonContent),
                        _ when dataPath.EndsWith("Recipes") => await _dataParser.ParseRecipes(jsonContent),
                        _ when dataPath.EndsWith("CraftingResources") => await _dataParser.ParseResources(jsonContent),
                        _ => []
                    };

                    data.ForEach(item => _itemData.TryAdd(item.Id, item));
                }

                fileName = dir.GetNext();
            }

            dir.ListDirEnd();
        }

        private IItem? TryGetItem(string id)
        {
            if (_itemData.TryGetValue(id, out var data)) return data;

            Tracker.TrackNotFound($"Item with id: {id}", this);
            return null;
        }
    }
}
