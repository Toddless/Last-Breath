namespace LastBreath.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Crafting.Source;
    using Godot;
    using Utilities;
    using EquipItem = Source.Items.EquipItem;
    using FileAccess = Godot.FileAccess;

    internal class ItemDataProvider : IItemDataProvider
    {
        private const string DataPath = "res://Data/";
        private readonly Dictionary<string, IItem> _itemData = [];
        private Dictionary<string, List<IModifier>> _equipItemModifierPools = [];
        private Dictionary<string, Dictionary<string, int>> _equipItemsResources = [];

        public IItem CopyItem(string id) => TryGetItem(id)?.Copy<IItem>() ?? throw new ArgumentNullException($"Item not found: {id}");

        public Texture2D? GetItemIcon(string id) => TryGetItem(id)?.Icon;

        public List<IModifier> GetEquipItemModifierPool(string id)
        {
            // Each item category has its own basic modifier pool. The first word in the ID represents the item category.
            string category = id.Split('_').First();
            if (!_equipItemModifierPools.TryGetValue(category, out List<IModifier>? modifiers))
                modifiers = [];
            if (!_equipItemModifierPools.TryGetValue(id, out var pool))
                pool = [];
            return modifiers.Concat(pool).ToList();
        }

        public List<IResourceRequirement> GetRecipeRequirements(string id)
        {
            var item = TryGetItem(id);
            return item is not ICraftingRecipe recipe ? [] : recipe.MainResource;
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
            if (item is not ICraftingRecipe recipe || recipe == null) throw new ArgumentNullException($"Recipe not found: {recipeId}");
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
                Tracker.TrackException("Data loading failed.", ex, this);
            }
        }

        private async Task LoadDataFromDirectory(string dataPath)
        {
            var equip = LoadDataFromJson(Path.Combine(dataPath, "EquipItems"), async s => await ParseEquipItems(s));
            var recipes = LoadDataFromJson(Path.Combine(dataPath, "Recipes"), async s => await ParseRecipes(s));
            var resources = LoadDataFromJson(Path.Combine(dataPath, "Resources"), async s => await ParseResources(s));
            var modifiers = LoadDataFromJson(Path.Combine(dataPath, "ModifierPools"), async s => await ParseModifiersPool(s));
            var equipResources = LoadDataFromJson(Path.Combine(dataPath, "EquipItemResources"), async s => await ParseEquipItemResources(s));

            await Task.WhenAll(equip, recipes, resources, modifiers, equipResources, modifiers);
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
                Tracker.TrackException("Failed to load equip items", e);
            }
            finally
            {
                dir.ListDirEnd();
            }
        }

        private async Task ParseEquipItems(string jsonContent)
        {
            List<IItem> data = await DataParser.ParseEquipItems(jsonContent,
                (equipType, id, tags) =>
                    new EquipItem(equipType, id, tags));
            lock (_itemData)
                data.ForEach(item => _itemData.TryAdd(item.Id, item));
        }

        private async Task ParseEquipItemResources(string jsonContent) => _equipItemsResources = await DataParser.ParseEquipItemResources(jsonContent);


        private async Task ParseModifiersPool(string jsonContent) =>
            await DataParser.ParseEquipItemModifierPools(jsonContent, ref _equipItemModifierPools,
                (parameter, type, value, weight) => new Modifier(type, parameter, value, weight));

        private async Task ParseResources(string jsonContent)
        {
            List<IItem> data = await DataParser.ParseResources<MaterialCategory>(
                jsonContent,
                (list, id) => new MaterialCategory(list, id),
                (id, tags, rarity, equipmentCategory, stackSize) => new UpgradeResource(id, tags, rarity, equipmentCategory, stackSize),
                (parameter, modifierType, baseValue, weight) => new MaterialModifier(parameter, modifierType, baseValue, weight),
                (materialModifiers, materialCategory) => new MaterialType(materialModifiers, materialCategory),
                (id, maxStackSize, tags, materialCategory, rarity) => new CraftingResource(id, maxStackSize, tags, materialCategory, rarity));
            lock (_itemData)
                data.ForEach(item => _itemData.TryAdd(item.Id, item));
        }

        private async Task ParseRecipes(string jsonContent)
        {
            var recipes = await DataParser.ParseRecipes<CraftingRecipe>(jsonContent,
                (type, s, arg3) => new ResourceRequirement(type, s, arg3),
                (id, resultItem, tags, rarity, requirements, isOpened) => new CraftingRecipe(id, resultItem, tags, rarity, requirements, isOpened));
            List<IItem> data = recipes.Cast<IItem>().ToList();
            lock (_itemData)
                data.ForEach(item => _itemData.TryAdd(item.Id, item));
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
