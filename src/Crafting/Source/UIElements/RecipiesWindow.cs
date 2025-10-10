namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Crafting.TestResources.Inventory;

    [Tool]
    [GlobalClass]
    public partial class RecipiesWindow : DraggableWindow, IInitializable, IClosable, IRequireServices
    {
        private const string UID = "uid://1qq3je71i5vi";
        private Dictionary<Categories, TreeItem> _categories = [];
        [Export] private RecipeTree? _recipeTree;
        [Export] private Button? _closeButton;
        private ItemDataProvider? _dataProvider;
        private ItemInventory? _inventory;
        private IItemCreator? _itemCreator;
        private UIElementProvider? _uIElementProvider;

        public event Action? Close;

        public override void _Ready()
        {
            if (_recipeTree != null)
            {
                _recipeTree.LeftClick += OnLeftClick;
                _recipeTree.RightClick += OnRightClick;
                _recipeTree.CtrLeftClick += OnCtrLeftClick;
            }
            if (_closeButton != null) _closeButton.Pressed += () => Close?.Invoke();
            if (DragArea != null) DragArea.GuiInput += DragWindow;
        }


        public void InjectServices(Core.Interfaces.Data.IServiceProvider provider)
        {
            _dataProvider = provider.GetService<ItemDataProvider>();
            _inventory = provider.GetService<ItemInventory>();
            _itemCreator = provider.GetService<IItemCreator>();
            _uIElementProvider = provider.GetService<UIElementProvider>();
        }

        public void CreateRecipeTree(IEnumerable<ICraftingRecipe> recipes)
        {
            if (_recipeTree == null)
            {
                Tracker.TrackNull(nameof(_recipeTree), this);
                return;
            }
            _recipeTree.Clear();
            _categories.Clear();

            var treeRoot = _recipeTree.CreateItem();
            foreach (var cat in Enum.GetValues<Categories>())
            {
                var category = _recipeTree.CreateItem(treeRoot);
                category.SetText(0, Lokalizator.Lokalize(cat.ToString()));
                category.SetSelectable(0, false);
                category.SetMetadata(0, "category");
                _categories[cat] = category;
            }

            foreach (var recipe in recipes)
            {
                var category = _categories.Keys.FirstOrDefault(category => recipe.Tags.Contains(category.ToString(), StringComparer.OrdinalIgnoreCase));
                var treeItem = _recipeTree.CreateItem(_categories[category]);
                string textAmount = GetAmountToCraftAsString(recipe.MainResource);
                treeItem.SetText(0, $"{Lokalizator.Lokalize(recipe.Id)} {textAmount}");
                treeItem.SetMetadata(0, recipe.Id);
                treeItem.SetSelectable(0, recipe.IsOpened);
            }
        }

        private string GetAmountToCraftAsString(IEnumerable<IResourceRequirement> requirements)
        {
            CalculateAmountToCraft(requirements, out var amount);
            return amount > 0 ? $"({amount})" : string.Empty;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnCtrLeftClick(string id, TreeItem treeItem)
        {
            var requrements = _dataProvider?.GetRecipeRequirements(id);
            var itemId = _dataProvider?.GetRecipeResultItemId(id);

            CalculateAmountToCraft(requrements ?? [], out var amount);
            if (amount > 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    var modifiers = new List<IMaterialModifier>();
                    foreach (var req in requrements ?? [])
                        modifiers.AddRange(_dataProvider?.GetResourceModifiers(req.ResourceId) ?? []);

                    var item = _itemCreator?.CreateEquipItem(itemId ?? string.Empty, modifiers);

                    if (item != null)
                    {
                        requrements?.ForEach(req => _inventory?.RemoveItemById(req.ResourceId, req.Amount));
                        item.SaveUsedResources(requrements?.ToDictionary(x => x.ResourceId, x => x.Amount) ?? []);
                        item.SaveModifiersPool(modifiers);
                    }
                }

                treeItem.SetText(0, $"{Lokalizator.Lokalize(id)} {GetAmountToCraftAsString(requrements ?? [])}");
            }
        }

        private void OnRightClick(string id, TreeItem treeItem)
        {
            var requrements = _dataProvider?.GetRecipeRequirements(id);
            var itemId = _dataProvider?.GetRecipeResultItemId(id);
            CalculateAmountToCraft(requrements ?? [], out var amount);
            if (amount > 1)
            {
                var modifiers = new List<IMaterialModifier>();
                foreach (var req in requrements ?? [])
                    modifiers.AddRange(_dataProvider?.GetResourceModifiers(req.ResourceId) ?? []);

                var item = _itemCreator?.CreateEquipItem(itemId ?? string.Empty, modifiers);

                if (item != null)
                {
                    requrements?.ForEach(req => _inventory?.RemoveItemById(req.ResourceId, req.Amount));
                    treeItem.SetText(0, $"{Lokalizator.Lokalize(id)} {GetAmountToCraftAsString(requrements ?? [])}");
                    item.SaveUsedResources(requrements?.ToDictionary(x => x.ResourceId, x => x.Amount) ?? []);
                    item.SaveModifiersPool(modifiers);
                }
            }
        }

        private void OnLeftClick(string id)
        {
            var craftingWindow = _uIElementProvider?.CreateSingleClosableOrGet<CraftingWindow>(GetParent());
            craftingWindow?.SetRecipeId(id);
        }

        private void CalculateAmountToCraft(IEnumerable<IResourceRequirement> requirements, out int amount)
        {
            int canCraft = int.MaxValue;
            foreach (var requirement in requirements)
            {
                var have = _inventory?.GetTotalItemAmount(requirement.ResourceId) ?? 0;
                int amountToCraft = have / requirement.Amount;
                canCraft = Mathf.Min(canCraft, amountToCraft);
            }
            amount = canCraft;
        }
    }
}
