namespace Core.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Constants;
    using Crafting;
    using Enums;
    using GameData;
    using Godot;
    using Interfaces;
    using Items;
    using Modifiers;

    /// <summary>The shared catalog owner: an <see cref="IGameDataParticipant"/> that, on load, feeds each file
    /// of the catalogs it declares to <see cref="IDataParser"/> and caches the parsed items/pools/costs by id.
    /// Read side of the item store — hands out independent copies via <see cref="CopyItem"/>.</summary>
    public sealed class ItemDataProvider(IDataParser dataParser, IReadOnlyList<string>? catalogs = null)
        : IItemDataProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, IItem> _itemData = [];
        // Equip templates never enter _itemData: they live as blueprints and are born through the minter only.
        private readonly Dictionary<string, EquipItemBlueprint> _blueprints = [];
        private readonly Dictionary<string, List<IModifierDescriptor>> _equipItemModifierPools = [];
        private Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<CostRequirement>>> _upgradeCosts = [];

        /// <summary>Catalogs every composition reads. Legacy items (<see cref="DataCatalog.Items"/>) are declared
        /// separately: a bootstrap whose <see cref="IItemGameDataFactory"/> cannot build them does not take the catalog.</summary>
        public static readonly string[] SharedCatalogs =
        [
            DataCatalog.EquipItems,
            DataCatalog.Recipes,
            DataCatalog.Resources,
            DataCatalog.ModifierPools,
            DataCatalog.UpgradeCosts,
        ];

        public IReadOnlyList<string> Catalogs { get; } = catalogs ?? SharedCatalogs;

        public void Apply(string catalog, GameDataFile file)
        {
            switch (catalog)
            {
                case DataCatalog.EquipItems:
                    AddBlueprints(dataParser.ParseEquipItems(file.Json));
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
                // TODO:
                // Данные предметы выбиваются из текущей архитектуры. Остаток старой системы. Убрать/переделать
                case DataCatalog.Items:
                    AddItems(dataParser.ParseItems(file.Json));
                    break;
                case DataCatalog.UpgradeCosts:
                    _upgradeCosts = dataParser.ParseUpgradeCosts(file.Json);
                    break;
            }
        }

        public IItem CopyItem(string id) => TryGetItem(id)?.Copy<IItem>() ?? throw new ArgumentNullException($"Item not found: {id}");

        public EquipItemBlueprint? GetBlueprint(string id) => _blueprints.GetValueOrDefault(id);

        public IEnumerable<EquipItemBlueprint> AllBlueprints => _blueprints.Values;

        // Equip icons resolve from the blueprint id (the same path a live EquipItem lazy-loads).
        public Texture2D? GetItemIcon(string id) =>
            _blueprints.ContainsKey(id)
                ? ResourceLoader.Load<Texture2D>(AssetPaths.ItemIcon(id))
                : TryGetItem(id)?.Icon;

        // Descriptors are immutable records — the cached lists can be handed out without defensive copies.
        public IReadOnlyList<IModifierDescriptor> GetEquipItemBaseModifierPool(string id) =>
            !_equipItemModifierPools.TryGetValue(id.Split('_')[0], out var modifiers) ? [] : modifiers;

        public IReadOnlyList<IModifierDescriptor> GetEquipItemModifierPool(string id) => !_equipItemModifierPools.TryGetValue(id, out var pool) ? [] : pool;

        public List<IRequirement> GetRecipeRequirements(string id)
        {
            var item = TryGetItem(id);
            return item is not ICraftingRecipe recipe ? [] : recipe.Requirements.Normalize(IsMaterialCategory);
        }

        public bool IsMaterialCategory(string id) => _itemData.Values
            .OfType<ICraftingResource>()
            .Any(resource => resource.Material?.MaterialCategory?.Id == id);

        public IReadOnlyList<string> GetResourceIdsInCategory(string categoryId) => [.. _itemData.Values
            .OfType<ICraftingResource>()
            .Where(resource => resource.Material?.MaterialCategory?.Id == categoryId)
            .Select(resource => resource.Id)];

        public IReadOnlyList<IRequirement> GetUpgradeCost(EquipmentCategory category, Rarity rarity) => GetCost(CraftingMode.Upgrade, category, rarity);
        public IReadOnlyList<IRequirement> GetRecraftCost(EquipmentCategory category, Rarity rarity) => GetCost(CraftingMode.Recraft, category, rarity);
        // Only Legendary items ascend, so the section resolves against that single rarity.
        public IReadOnlyList<IRequirement> GetAscendCost(EquipmentCategory category) => GetCost(CraftingMode.Ascend, category, Rarity.Legendary);

        public string GetRecipeResultItemId(string recipeId)
        {
            var item = TryGetItem(recipeId);
            return item is not ICraftingRecipe recipe ? string.Empty : recipe.ResultItemId;
        }

        public IReadOnlyList<IModifierDescriptor> GetResourceDescriptors(string id)
        {
            if (!_itemData.TryGetValue(id, out var res) || res is not ICraftingResource crafting) return [];
            return crafting.Material?.Modifiers ?? [];
        }

        public ICraftingRecipe GetRecipe(string recipeId)
        {
            var item = TryGetItem(recipeId);
            return item as ICraftingRecipe ?? throw new ArgumentNullException($"Recipe not found: {recipeId}");
        }

        public bool IsItemHasTag(string id, string tag) =>
            _blueprints.TryGetValue(id, out var blueprint)
                ? blueprint.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                : TryGetItem(id)?.HasTag(tag) ?? false;

        public IEnumerable<IItem> GetAllResources() => [.. _itemData.Values.Where(x => x is IResource)];

        public IEnumerable<ICraftingRecipe> GetCraftingRecipes() => [.. _itemData.Values.Where(x => x is ICraftingRecipe).Cast<ICraftingRecipe>()];

        private void AddItems(List<IItem> data) => data.ForEach(item => _itemData.TryAdd(item.Id, item));

        private void AddBlueprints(List<EquipItemBlueprint> data) => data.ForEach(blueprint => _blueprints.TryAdd(blueprint.Id, blueprint));

        private IReadOnlyList<IRequirement> GetCost(CraftingMode mode, EquipmentCategory category, Rarity rarity) =>
            _upgradeCosts.TryGetValue(mode, out var categories) && categories.TryGetValue(category, out var requirements)
                ? requirements.Select(requirement => requirement.Resolve(rarity)).OfType<IRequirement>().ToList()
                : [];

        private IItem? TryGetItem(string id)
        {
            if (_itemData.TryGetValue(id, out var data)) return data;

            Tracker.TrackNotFound($"Item with id: {id}", this);
            return null;
        }
    }
}
