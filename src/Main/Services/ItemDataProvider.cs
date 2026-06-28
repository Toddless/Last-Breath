namespace LastBreath.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Godot;
    using Utilities;
    using FileAccess = Godot.FileAccess;

    internal class ItemDataProvider : IItemDataProvider
    {
        private const string DataPath = "res://Data/";
        private readonly Dictionary<string, IItem> _itemData = [];
        private Dictionary<string, List<IModifier>> _equipItemModifierPools = [];
        private Dictionary<string, Dictionary<string, int>> _equipItemsResources = [];
        private readonly IDataParser _dataParser;

        public ItemDataProvider(IDataParser dataParser)
        {
            _dataParser = dataParser;
            LoadData();
        }

        public IItem CopyItem(string id) => TryGetItem(id)?.Copy<IItem>() ?? throw new ArgumentNullException($"Item not found: {id}");

        public Texture2D? GetItemIcon(string id) => TryGetItem(id)?.Icon;

        public List<IModifier> GetEquipItemBaseModifierPool(string id) =>
            !_equipItemModifierPools.TryGetValue(id.Split('_')[0], out List<IModifier>? modifiers) ? [] : modifiers.ToList();

        public List<IModifier> GetEquipItemModifierPool(string id) => !_equipItemModifierPools.TryGetValue(id, out var pool) ? [] : pool.ToList();

        public List<IRequirement> GetRecipeRequirements(string id)
        {
            var item = TryGetItem(id);
            return item is not ICraftingRecipe recipe ? [] : recipe.Requirements;
        }

        public Dictionary<string, int> GetEquipItemResources(string itemId) =>
            _equipItemsResources.TryGetValue(itemId, out var res) ? res.ToDictionary() : [];

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
                await LoadDataFromDirectory(DataPath);
            }
            catch (Exception ex)
            {
                GD.Print($"Failed to load data: {ex.Message} \n {ex.StackTrace}");
                Tracker.TrackException("Data loading failed.", ex, this);
            }
        }

        private async Task LoadDataFromDirectory(string dataPath)
        {
            var equip = LoadDataFromJson(Path.Combine(dataPath, "EquipItems"), async s => AddItems(await _dataParser.ParseEquipItems(s)));
            var recipes = LoadDataFromJson(Path.Combine(dataPath, "Recipes"), async s => AddItems(await _dataParser.ParseRecipes(s)));
            var resources = LoadDataFromJson(Path.Combine(dataPath, "Resources"), async s => AddItems(await _dataParser.ParseResources(s)));
            var modifiers = LoadDataFromJson(Path.Combine(dataPath, "ModifierPools"), async s => await _dataParser.ParseEquipItemModifierPools(s, ref _equipItemModifierPools));
            var equipResources = LoadDataFromJson(Path.Combine(dataPath, "EquipItemResources"),
                async s => { _equipItemsResources = await _dataParser.ParseEquipItemResources(s); });
            var items = LoadDataFromJson(Path.Combine(dataPath, "Items"), async s => AddItems(await _dataParser.ParseItems(s)));

            await Task.WhenAll(equip, recipes, resources, modifiers, equipResources, items);
        }

        private void AddItems(List<IItem> data)
        {
            lock (_itemData)
                data.ForEach(item => _itemData.TryAdd(item.Id, item));
        }

        private static async Task LoadDataFromJson(string path, Func<string, Task> loadDataFunc)
        {
            var dir = DirAccess.Open(path);
            dir.ListDirBegin();
            try
            {
                string fileName = dir.GetNext();
                while (fileName != string.Empty)
                {
                    string filePath = Path.Combine(path, fileName);
                    if (!filePath.EndsWith(".json")) continue;
                    using var openFile = FileAccess.Open(filePath, FileAccess.ModeFlags.Read) ?? throw new FileLoadException();
                    string jsonContent = openFile.GetAsText() ?? throw new FileLoadException();
                    await loadDataFunc(jsonContent);
                    fileName = dir.GetNext();
                }
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to load item data", e);
            }
            finally
            {
                dir.ListDirEnd();
            }
        }

        private IItem? TryGetItem(string id)
        {
            if (_itemData.TryGetValue(id, out var data)) return data;

            Tracker.TrackNotFound($"Item with id: {id}", this);
            GD.Print($"Item with id: {id} not found");
            return null;
        }
    }
}
