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
        private long _lastSelectedModifierInd = -1;
        private List<OptionalResource> _optionalResources = [];
        private List<MainResource> _mainResources = [];
        private Dictionary<Categories, TreeItem> _categories = [];
        [Export] private Tree? _recipeTree;
        [Export] private ItemList? _itemModifierList;
        [Export] private VBoxContainer? _optional, _main;
        [Export] private Button? _action;
        [Export] private RichTextLabel? _description;
        [Export] private GridContainer? _inventoryGrid;
        [Export] private Label? _randomLabel;
        [Export] private RichTextLabel? _baseStats, _possibleModifiersList;
        [Export] private CraftingSlot? _slot;

        [Signal] public delegate void RecipeSelectedEventHandler(string id);
        [Signal] public delegate void ActionButtonPressedEventHandler();
        [Signal] public delegate void ModifierSelectedEventHandler(int hash, string itemInstanceId);
        [Signal] public delegate void OnRemoveRecipeEventHandler();
        [Signal] public delegate void OnEquipItemPlacedEventHandler(string instanceId);
        [Signal] public delegate void OnEquipItemRemovedEventHandler();
        [Signal] public delegate void OnEquipItemReturnedEventHandler(string itemId, string instanceId);

        public override void _Ready()
        {
            _recipeTree?.Clear();
            if (_recipeTree != null) _recipeTree.ItemSelected += OnItemSelected;
            if (_action != null)
            {
                _action.Disabled = true;
                _action.Pressed += () => EmitSignal(SignalName.ActionButtonPressed);
            }
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
                Logger.LogNull(nameof(_recipeTree), this);
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

        public void ConsumeOptionalResource() => ConsumeResources(_optionalResources);

        public void ConsumeMainResources() => ConsumeResources(_mainResources);

        public void ShowModifiers(string formatted)
        {
            if (_possibleModifiersList != null) _possibleModifiersList.Text = formatted;
        }

        public void SetItemDescription(string text)
        {
            if (_description != null) _description.Text = text;
        }

        public void SetActionButtonState(bool canUse)
        {
            if (_action != null) _action.Disabled = !canUse;
        }

        public void SetActionButtonName(string text)
        {
            if (_action != null) _action.Text = text;
        }

        public void SetRandomStatsLabel(string text)
        {
            if (_randomLabel != null) _randomLabel.Text = text;
        }

        public void SetBaseStats(string text)
        {
            if (_baseStats != null) _baseStats.Text = text;
        }

        public void SetItemModifiers(List<(string Mod, int Hash)> modifiers)
        {
            if (_itemModifierList != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    _itemModifierList.AddItem(modifiers[i].Mod);
                    _itemModifierList.SetItemMetadata(i, modifiers[i].Hash);
                    // TODO: Not all mods can be changed
                    _itemModifierList.SetItemSelectable(i, true);
                }
            }
        }

        public void SetInCraftingSlot(ItemInstance instance)
        {
            if (_slot != null) _slot.SetItem(instance);
        }

        public void ClearPossibleModifiers()
        {
            if (_possibleModifiersList != null) _possibleModifiersList.Text = string.Empty;
        }
        public void ClearItemBaseStats()
        {
            if (_baseStats != null) _baseStats.Text = string.Empty;
        }
        public void ClearItemModifiers()
        {
            _lastSelectedModifierInd = -1;
            _itemModifierList?.Clear();
        }
        public void ClearOptionalResources() => _optionalResources.ForEach(x => x.RemoveCraftingResource());
        public void ClearMainResources()
        {
            foreach (var child in _main?.GetChildren() ?? [])
                child.QueueFree();
            _mainResources.Clear();
        }
        public void DestroyRecipeTree() => _recipeTree?.Clear();
        public void DeselecteAllRecipe() => _recipeTree?.DeselectAll();
        public void ClearDescription()
        {
            if (_description != null) _description.Text = string.Empty;
        }

        private void ConsumeResources<T>(List<T> resource)
            where T : BaseResource
        {
            foreach (var res in resource)
            {
                res.ConsumeResource();
                if (res.CanClear()) res.RemoveCraftingResource();
            }
        }

        private void OnItemSelected()
        {
            var selected = _recipeTree?.GetSelected();
            if (selected == null) return;
            var selectedRecipeId = selected.GetMetadata(0);
            EmitSignal(SignalName.RecipeSelected, selectedRecipeId);
        }

        private void OnModifierItemSelected(long index)
        {
            if (index == _lastSelectedModifierInd)
            {
                _itemModifierList?.Deselect((int)index);
                _lastSelectedModifierInd = -1;
                return;
            }
            _lastSelectedModifierInd = index;
            var hash = _itemModifierList?.GetItemMetadata((int)index).AsInt32() ?? 0;
            EmitSignal(SignalName.ModifierSelected, hash, _slot?.CurrentItem?.InstanceId ?? string.Empty);
        }
    }
}
