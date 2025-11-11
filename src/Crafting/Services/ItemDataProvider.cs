namespace Crafting.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Godot;
    using Newtonsoft.Json;
    using TestResources;
    using Utilities;

    internal class ItemDataProvider(string itemDataPath) : IItemDataProvider
    {
        private readonly Dictionary<string, IItem> _itemData = [];
        private readonly Dictionary<string, List<IModifier>> _itemBaseStatsData = [];

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
                    using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read) ??
                                     throw new FileLoadException();
                    string jsonContent = file.GetAsText() ?? throw new FileNotFoundException();
                    List<IItem> data = [];
                    switch (true)
                    {
                        case var _ when dataPath.EndsWith("EquipItems"):
                            data = await DataParser.ParseEquipItems(jsonContent);
                            break;
                        case var _ when dataPath.EndsWith("Recipes"):
                            data = await DataParser.ParseRecipes(jsonContent);
                            break;
                        case var _ when dataPath.EndsWith("Resources"):
                            data = await DataParser.ParseResources(jsonContent);
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
            if (!_itemData.TryGetValue(id, out var data))
            {
                Tracker.TrackNotFound($"Item with id: {id}", this);
                return null;
            }
            return data;
        }
    }
}
