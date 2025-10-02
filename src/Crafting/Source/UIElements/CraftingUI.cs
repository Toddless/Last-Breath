namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Crafting.TestResources.Inventory;
    using Godot;
    using Utilities;

    public partial class CraftingUI : Control
    {
        private List<OptionalResource> _optionalResources = [];
        private List<MainResource> _mainResources = [];
        private Dictionary<Categories, TreeItem> _categories = [];
        [Export] private Tree? _recipeTree;
        [Export] private ItemModifierList? _itemModifierList;
        [Export] private VBoxContainer? _optional, _main;
        [Export] private GridContainer? _inventoryGrid;
        [Export] private RichTextLabel? _baseStats, _possibleModifiersList;
        [Export] private CraftingSlot? _slot;
        [Export] private Container? _additionalActionsContainer;

        [Signal] public delegate void RecipeSelectedEventHandler(string id);
        [Signal] public delegate void ModifierSelectedEventHandler(int hash, string itemInstanceId);
        [Signal] public delegate void OnEquipItemPlacedEventHandler(string instanceId);
        [Signal] public delegate void OnEquipItemRemovedEventHandler();
        [Signal] public delegate void OnEquipItemReturnedEventHandler(string itemId, string instanceId);

        public override void _Ready()
        {
            _recipeTree?.Clear();
            if (_recipeTree != null) _recipeTree.ItemSelected += OnRecipeSelected;
            if (_itemModifierList != null)
            {
                _itemModifierList.ItemSelected += OnModifierItemSelected;
            }
            if (_slot != null)
            {
                _slot.EquipItemPlaced += (id) => EmitSignal(SignalName.OnEquipItemPlaced, id);
                _slot.EquipItemRemoved += () => EmitSignal(SignalName.OnEquipItemRemoved);
                _slot.EquipItemReturned += (id, instanceId) => EmitSignal(SignalName.OnEquipItemReturned, id, instanceId);
            }
        }

        public void InitializeInventory(ItemInventory inventory)
        {
            inventory.Initialize(216, _inventoryGrid);
        }

        public void CreatingRecipeTree(IEnumerable<ICraftingRecipe> recipes)
        {
            if (_recipeTree == null)
            {
                Tracker.TrackNull(nameof(_recipeTree), this);
                return;
            }

            _categories.Clear();

            var treeRoot = _recipeTree.CreateItem();
            foreach (var cat in Enum.GetValues<Categories>())
            {
                var category = _recipeTree.CreateItem(treeRoot);
                category.SetText(0, Lokalizator.Lokalize(cat.ToString()));
                category.SetSelectable(0, false);
                _categories[cat] = category;
            }

            foreach (var recipe in recipes)
            {
                var category = _categories.Keys.FirstOrDefault(category => recipe.Tags.Contains(category.ToString(), StringComparer.OrdinalIgnoreCase));
                var treeItem = _recipeTree.CreateItem(_categories[category]);
                treeItem.SetText(0, Lokalizator.Lokalize(recipe.Id));
                treeItem.SetMetadata(0, recipe.Id);
                treeItem.SetSelectable(0, recipe.IsOpened);
            }
        }

        public void AddOptionalResource(OptionalResource optional)
        {
            _optional?.AddChild(optional);
            _optionalResources.Add(optional);
        }

        public void AddMainResource(MainResource res)
        {
            _main?.AddChild(res);
            _mainResources.Add(res);
        }

        public void AddActionNode(Node node) => _additionalActionsContainer?.AddChild(node);

        public void ConsumeOptionalResource() => ConsumeResources(_optionalResources);

        public void ConsumeMainResources() => ConsumeResources(_mainResources);

        public void ShowPossibleModifiers(string formatted)
        {
            if (_possibleModifiersList != null) _possibleModifiersList.Text = formatted;
        }

        public ItemInstance? GetCraftingSlotItem() => _slot?.CurrentItem;
        public void SetItemDescription(RichTextLabel text) => _additionalActionsContainer?.AddChild(text);

        public void SetBaseStats(string text)
        {
            if (_baseStats != null) _baseStats.Text = text;
        }

        public void SetItemModifiers(List<(string Mod, int Hash)> modifiers) => _itemModifierList?.AddModifiersToList(modifiers);
        public void SetItemModifiersSelectable(bool selectable) => _itemModifierList?.SetItemSelectable(selectable);
        public void ShowRecipe(string id) => _slot?.SetRecipe(id);
        public void UpdateSelectedItemModifer((string Modifier, int Hash) mod) => _itemModifierList?.UpdateSelectedItem(mod);
        public void ClearCraftingSlot() => _slot?.ClearSlot();
        public void ClearPossibleModifiers()
        {
            if (_possibleModifiersList != null) _possibleModifiersList.Text = string.Empty;
        }
        public void ClearItemBaseStats()
        {
            if (_baseStats != null) _baseStats.Text = string.Empty;
        }
        public void ClearItemModifiers() => _itemModifierList?.ClearList();
        public void ClearOptionalResources() => _optionalResources.ForEach(x => x.RemoveCraftingResource());
        public void ClearMainResources()
        {
            FreeChildren(_main);
            _mainResources.Clear();
        }
        public void ClearActions() => FreeChildren(_additionalActionsContainer);
        public void DestroyRecipeTree() => _recipeTree?.Clear();
        public void DeselecteAllRecipe() => _recipeTree?.DeselectAll();

        public void ClearDescription() => FreeChildren(_additionalActionsContainer);

        private void ConsumeResources<T>(List<T> resources)
            where T : BaseResource
        {
            foreach (var res in resources)
            {
                res.ConsumeResource();
                if (res.CanClear()) res.RemoveCraftingResource();
            }
        }

        private void FreeChildren(Node? node)
        {
            foreach (var child in node?.GetChildren() ?? [])
                child.QueueFree();
        }

        private void OnRecipeSelected()
        {
            var selected = _recipeTree?.GetSelected();
            if (selected == null) return;
            var selectedRecipeId = selected.GetMetadata(0);
            EmitSignal(SignalName.RecipeSelected, selectedRecipeId);
        }

        private void OnModifierItemSelected(int hash) => EmitSignal(SignalName.ModifierSelected, hash, _slot?.CurrentItem?.InstanceId ?? string.Empty);
    }
}
