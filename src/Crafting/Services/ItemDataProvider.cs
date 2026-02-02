namespace Crafting.Services
{
    using Godot;
    using System;
    using Source;
    using Utilities;
    using System.IO;
    using System.Linq;
    using TestResources;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Modifiers;

    internal class ItemDataProvider(string itemDataPath) : IItemDataProvider
    {
        private readonly Dictionary<string, IItem> _itemData = [];
        private readonly Dictionary<string, List<IModifier>> _equipItemModifierPools = [];

        public IItem CopyBaseItem(string id) => TryGetItem(id)?.Copy<IItem>() ?? throw new ArgumentNullException($"Item not found: {id}");

        public Texture2D? GetItemIcon(string id) => TryGetItem(id)?.Icon;

        public List<IModifier> GetEquipItemModifierPool(string id)
        {
            if (!_equipItemModifierPools.TryGetValue(id, out var data)) return [];
            return [.. data];
        }

        public Dictionary<string, int> GetEquipItemResources(string itemId) => throw new NotImplementedException();

        public List<IResourceRequirement> GetRecipeRequirements(string id)
        {
            var item = TryGetItem(id);
            return item is not ICraftingRecipe recipe ? [] : recipe.MainResource;
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
                await LoadDataFromDirectory(itemDataPath);
            }
            catch (Exception ex)
            {
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
                string filePath = Path.Combine(dataPath, fileName);
                if (dir.CurrentIsDir())
                    await LoadDataFromDirectory(filePath);
                else
                {
                    if (!filePath.EndsWith(".json")) continue;
                    using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read) ?? throw new FileLoadException();
                    string jsonContent = file.GetAsText() ?? throw new FileNotFoundException();
                    List<IItem> data = [];
                    switch (true)
                    {
                        case var _ when dataPath.EndsWith("EquipItems"):
                            data = await DataParser.ParseEquipItems(jsonContent,
                                (equipType, id, tags) =>
                                    new TestEquipItem(equipType, id, tags));
                            break;
                        case var _ when dataPath.EndsWith("Recipes"):
                            var recipes = await DataParser.ParseRecipes<CraftingRecipe>(jsonContent,
                                (type, s, arg3) => new ResourceRequirement(type, s, arg3),
                                (id, resultItem, tags, rarity, requirements, isOpened) => new CraftingRecipe(id, resultItem, tags, rarity, requirements, isOpened));
                            data = recipes.Cast<IItem>().ToList();
                            break;
                        case var _ when dataPath.EndsWith("Resources"):
                            data = await DataParser.ParseResources<MaterialCategory>(
                                jsonContent,
                                (list, id) => new MaterialCategory(list, id),
                                (id, tags, rarity, equipmentCategory, stackSize) => new UpgradeResource(id, tags, rarity, equipmentCategory, stackSize),
                                (parameter, modifierType, baseValue, weight) => new MaterialModifier(parameter, modifierType, baseValue, weight),
                                (materialModifiers, materialCategory) => new MaterialType(materialModifiers, materialCategory),
                                (id, maxStackSize, tags, materialCategory, rarity) => new CraftingResource(id, maxStackSize, tags, materialCategory, rarity));
                            break;
                    }

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
