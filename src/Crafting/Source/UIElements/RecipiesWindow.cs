namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Requests;

    [Tool]
    [GlobalClass]
    public partial class RecipiesWindow : DraggableWindow, IInitializable, IClosable, IRequireServices
    {
        private const string UID = "uid://1qq3je71i5vi";
        private Dictionary<Categories, TreeItem> _categories = [];
        [Export] private RecipeTree? _recipeTree;
        [Export] private Button? _closeButton;

        private IItemDataProvider? _dataProvider;
        private IInventory? _inventory;
        private IUiMediator? _uiMediator;
        private ISystemMediator? _systemMediator;

        public event Action? Close;

        public override void _Ready()
        {
            if (_recipeTree != null)
            {
                _recipeTree.LeftClick += OnLeftClick;
                _recipeTree.RightClick += OnRightClick;
            }
            if (_closeButton != null) _closeButton.Pressed += () => Close?.Invoke();
            if (DragArea != null) DragArea.GuiInput += DragWindow;
        }

        public void InjectServices(Core.Interfaces.Data.IServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _inventory = provider.GetService<IInventory>();
            _uiMediator = provider.GetService<IUiMediator>();
            _systemMediator = provider.GetService<ISystemMediator>();
            _uiMediator.UpdateUi += UpdateRecipeTree;
        }

        public override void _ExitTree()
        {
            if (_uiMediator != null) _uiMediator.UpdateUi -= UpdateRecipeTree;
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
                CalculateAmountToCraft(recipe.MainResource, out var amount);
                var recipeName = $"{Lokalizator.Lokalize(recipe.Id)}";
                if (amount > 0)
                    recipeName += $" ({amount})";
                treeItem.SetText(0, recipeName);
                treeItem.SetMetadata(0, recipe.Id);
                treeItem.SetSelectable(0, recipe.IsOpened);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnRightClick(string id, TreeItem treeItem)
        {
            var requrements = _dataProvider?.GetRecipeRequirements(id);
            var itemId = _dataProvider?.GetRecipeResultItemId(id);

            CalculateAmountToCraft(requrements ?? [], out var amount);
            if (amount > 1 && requrements != null)
            {
                var modifiers = new List<IMaterialModifier>();
                foreach (var req in requrements)
                    modifiers.AddRange(_dataProvider?.GetResourceModifiers(req.ResourceId) ?? []);

                _systemMediator?.Send(new CreateEquipItemRequest(itemId ?? string.Empty, modifiers, requrements.ToDictionary(key => key.ResourceId, value => value.Amount)));
            }
        }

        private void OnLeftClick(string id) => _uiMediator?.Send(new OpenWindowRequest(typeof(CraftingWindow), id));

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

        private void UpdateRecipeTree()
        {
            var root = _recipeTree?.GetRoot();
            UpdateTreeItem(root);

            void UpdateTreeItem(TreeItem? item)
            {
                if(item == null) return;

                string meta = item.GetMetadata(0).AsString();
                if (!meta.Equals("category", StringComparison.OrdinalIgnoreCase))
                {
                    CalculateAmountToCraft(_dataProvider?.GetRecipeRequirements(meta) ?? [], out var amount);
                    var recipeName = $"{Lokalizator.Lokalize(meta)}";
                    if (amount > 0)
                        recipeName += $" ({amount})";
                    item.SetText(0, recipeName);
                }

                var child = item.GetChildren();
                foreach (var c in child)
                    UpdateTreeItem(c);
            }
        }
    }
}
