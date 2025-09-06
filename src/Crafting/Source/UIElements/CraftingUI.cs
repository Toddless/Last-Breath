namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Crafting.TestResources.Inventory;
    using Godot;
    using Utilities;

    public partial class CraftingUI : Control
    {
        private List<OptionalResource> _optionalResources = [];
        private Dictionary<Categories, TreeItem> _categories = [];
        [Export] private Tree? _recipeTree;
        [Export] private ItemList? _possibleModifiersList;
        [Export] private VBoxContainer? _main, _optional;
        [Export] private Button? _create, _changeLanguage;
        [Export] private TextureRect? _iconRect;
        [Export] private VBoxContainer? _itemBaseStatsContainer;
        [Export] private RichTextLabel? _description;
        [Export] private GridContainer? _inventoryGrid;

        [Signal] public delegate void RecipeSelectedEventHandler(string id);
        [Signal] public delegate void ItemCreatedEventHandler();
        [Signal] public delegate void ChangeLanguageEventHandler();

        public override void _Ready()
        {
            _recipeTree?.Clear();
            if (_recipeTree != null) _recipeTree.ItemSelected += OnItemSelected;
            if (_create != null)
            {
                _create.Disabled = true;
                _create.Pressed += () => EmitSignal(SignalName.ItemCreated);
            }

            if (_changeLanguage != null)
            {
                _changeLanguage.Pressed += () => EmitSignal(SignalName.ChangeLanguage);
            }
        }

        public void InitializeInventory(ItemInventory inventory)
        {
            inventory.Initialize(216, _inventoryGrid);
        }

        public void CreatingRecipeTree(IEnumerable<ICraftingRecipe>? recipes)
        {
            if (_recipeTree == null)
            {
                Logger.LogNull(nameof(_recipeTree), this);
                return;
            }

            if (recipes == null)
            {
                Logger.LogNull(nameof(recipes), this);
                return;
            }

            var treeRoot = _recipeTree.CreateItem();

            foreach (var cat in Enum.GetValues<Categories>())
            {
                var category = _recipeTree.CreateItem(treeRoot);
                category.SetText(0, Lokalizator.Lokalize(cat.ToString()));
                category.SetSelectable(0, false);
                _categories[cat] = category;
            }

            foreach (var res in recipes)
            {
                var category = _categories.Keys.FirstOrDefault(x => res.Tags.Contains(x.ToString()));
                var recipe = _recipeTree.CreateItem(_categories[category]);
                recipe.SetText(0, Lokalizator.Lokalize(res.Id));
                recipe.SetMetadata(0, res.Id);
                recipe.SetSelectable(0, true);
            }

        }

        public void AddOptionalResource(OptionalResource optional)
        {
            _optional?.AddChild(optional);
            _optionalResources.Add(optional);
        }

        public void AddBaseStatLabel(Label label) => _itemBaseStatsContainer?.AddChild(label);

        public void ConsumeOptionalResource()
        {
            foreach (var resource in _optionalResources)
            {
                resource.ConsumeResource();
                if (resource.CanClear()) resource.RemoveCraftingResource();
            }
        }

        public void ShowRecipe(IEnumerable<ResourceTemplateUI> resources)
        {
            // TODO: Remember weights
            ClearResources(_main);
            foreach (var resource in resources)
                _main?.AddChild(resource);
        }

        public void ShowModifiers(IEnumerable<string> formatted)
        {
            _possibleModifiersList?.Clear();
            foreach (var text in formatted)
                _possibleModifiersList?.AddItem(text);
        }

        public void SetItemDescription(string text)
        {
            if (_description != null) _description.Text = text;
        }

        public void SetCreateButtonState(bool canUse)
        {
            if (_create != null) _create.Disabled = !canUse;
        }

        public void SetItemIcon(Texture2D icon)
        {
            if (_iconRect != null) _iconRect.Texture = icon;
        }

        public void ClearUI()
        {
            ClearResources(_main);
            ClearOptionalResources();
            ClearItemIcon();
            DestroyRecipeTree();
            SetCreateButtonState(false);
            _possibleModifiersList?.Clear();
            ClearDescription();
        }

        public void ClearOptionalResources() => _optionalResources.ForEach(x => x.RemoveCraftingResource());

        public void ClearPossibleModifiers() => _possibleModifiersList?.Clear();

        public void ClearDescription()
        {
            if (_description != null) _description.Text = string.Empty;
        }

        public void ClearItemIcon()
        {
            if (_iconRect != null) _iconRect.Texture = default;
        }

        public void DestroyRecipeTree() => _recipeTree?.Clear();

        private void ClearResources(VBoxContainer? container)
        {
            foreach (var child in container?.GetChildren() ?? []) child.QueueFree();
            foreach (var text in _itemBaseStatsContainer?.GetChildren() ?? []) text.QueueFree();
        }

        private void OnItemSelected()
        {
            var selected = _recipeTree?.GetSelected();
            if (selected == null) return;
            EmitSignal(SignalName.RecipeSelected, selected.GetMetadata(0));
        }
    }
}
