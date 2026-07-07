namespace Crafting.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Items;
    using Core.Modifiers;
    using Godot;

    internal class ItemDataProvider(IDataParser dataParser) : IItemDataProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, IItem> _itemData = [];
        private readonly Dictionary<string, List<IModifier>> _equipItemModifierPools = [];
        private Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<IRequirement>>> _upgradeCosts = [];

        public IReadOnlyList<string> Catalogs =>
        [
            DataCatalog.EquipItems,
            DataCatalog.Recipes,
            DataCatalog.Resources,
            DataCatalog.ModifierPools,
            DataCatalog.UpgradeCosts,
        ];

        public void Apply(string catalog, GameDataFile file)
        {
            switch (catalog)
            {
                case DataCatalog.EquipItems:
                    AddItems(dataParser.ParseEquipItems(file.Json));
                    break;
                case DataCatalog.Recipes:
                    AddItems(dataParser.ParseRecipes(file.Json));
                    break;
                case DataCatalog.Resources:
                    AddItems(dataParser.ParseResources(file.Json));
                    break;
                case DataCatalog.ModifierPools:
                    foreach ((string id, var pool) in dataParser.ParseEquipItemModifierPools(file.Json))
                        _equipItemModifierPools.TryAdd(id, pool);
                    break;
                case DataCatalog.UpgradeCosts:
                    _upgradeCosts = dataParser.ParseUpgradeCosts(file.Json);
                    break;
            }
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

        public IReadOnlyList<IRequirement> GetUpgradeCost(EquipmentCategory category) => GetCost(CraftingMode.Upgrade, category);
        public IReadOnlyList<IRequirement> GetRecraftCost(EquipmentCategory category) => GetCost(CraftingMode.Recraft, category);

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

        private void AddItems(List<IItem> data) => data.ForEach(item => _itemData.TryAdd(item.Id, item));

        private IReadOnlyList<IRequirement> GetCost(CraftingMode mode, EquipmentCategory category) =>
            _upgradeCosts.TryGetValue(mode, out var categories) && categories.TryGetValue(category, out var requirements) ? requirements : [];

        private IItem? TryGetItem(string id)
        {
            if (_itemData.TryGetValue(id, out var data)) return data;

            Tracker.TrackNotFound($"Item with id: {id}", this);
            return null;
        }
    }
}
