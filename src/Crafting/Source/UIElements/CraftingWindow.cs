namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Results;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The one crafting screen: recipe tree with search on the left, the item and its costs on
    /// the right, one action button for all four modes (Create / Upgrade / Recraft / Ascend).
    /// Reads go straight to the services; only the crafting COMMANDS travel the bus. Rows are
    /// plain code-built controls — the scene holds containers, not slots.
    /// </summary>
    public partial class CraftingWindow : Control, IWindow
    {
        private const string UID = "uid://betq124kfglyy";
        private const int AdditiveSlots = 3;

        [Export] private Tree? _tree;
        [Export] private LineEdit? _search;
        [Export] private Button? _close, _actionButton;
        [Export] private TextureRect? _itemIcon;
        [Export] private Label? _itemName, _itemSubtitle, _modsHeader, _additivesHeader;
        [Export] private VBoxContainer? _mods, _requirements;
        [Export] private HBoxContainer? _additives;
        [Export] private Control? _picker;
        [Export] private Label? _pickerTitle;
        [Export] private ItemList? _pickerList;
        [Export] private Button? _pickerCancel;

        private readonly Dictionary<string, string> _categoryChoices = [];
        private readonly string?[] _additiveChoices = new string?[AdditiveSlots];
        private readonly List<string> _pickerIds = [];
        private Action<string>? _pickerCallback;

        private IItemDataProvider? _dataProvider;
        private IInventory? _inventory;
        private ICraftingMastery? _mastery;
        private IItemUpgrader? _upgrader;
        private IItemAscender? _ascender;
        private ICraftingAdditiveProvider? _additiveProvider;
        private IGameMessageBus? _messageBus;

        private CraftingMode _mode = CraftingMode.Create;
        private string? _recipeId;
        private IEquipItem? _item;
        private int? _selectedModifierHash;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _tree?.ItemSelected += OnRecipeSelected;
            _search?.TextChanged += _ => BuildTree();
            _close?.Pressed += Close;
            _actionButton?.Pressed += OnActionPressed;
            _pickerCancel?.Pressed += ClosePicker;
            _pickerList?.ItemActivated += OnPickerActivated;
        }

        public override void _ExitTree()
        {
            if (_inventory != null) _inventory.ItemAmountChanges -= OnItemAmountChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _inventory = provider.GetService<IInventory>();
            _mastery = provider.GetService<ICraftingMastery>();
            _upgrader = provider.GetService<IItemUpgrader>();
            _ascender = provider.GetService<IItemAscender>();
            _additiveProvider = provider.GetService<ICraftingAdditiveProvider>();
            _messageBus = provider.GetService<IGameMessageBus>();

            _inventory.ItemAmountChanges += OnItemAmountChanged;
            BuildTree();
            RefreshDetails();
        }

        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>Entry point for item operations (inventory tooltip buttons and the like).</summary>
        public void SetItem(IEquipItem item, CraftingMode mode)
        {
            _item = item;
            _mode = mode;
            _recipeId = null;
            _selectedModifierHash = null;
            ResetChoices();
            _tree?.DeselectAll();
            RefreshDetails();
        }

        // ---------------------------------------------------------------- tree

        private void BuildTree()
        {
            if (_tree == null || _dataProvider == null) return;

            string query = _search?.Text?.Trim() ?? string.Empty;
            _tree.Clear();
            _tree.HideRoot = true;
            var root = _tree.CreateItem();

            foreach (var group in _dataProvider.GetCraftingRecipes()
                         .GroupBy(RecipeCategory)
                         .OrderBy(entry => entry.Key.ToString()))
            {
                var matching = group
                    .Where(recipe => query.Length == 0 || Localization.Localize(recipe.Id).Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matching.Count == 0) continue;

                var category = _tree.CreateItem(root);
                category.SetText(0, Localization.Localize(group.Key.ToString()));
                category.SetSelectable(0, false);

                foreach (var recipe in matching)
                {
                    var entry = _tree.CreateItem(category);
                    int amount = CraftableAmount(recipe.Id);
                    entry.SetText(0, amount > 0 ? $"{Localization.Localize(recipe.Id)} ({amount})" : Localization.Localize(recipe.Id));
                    entry.SetMetadata(0, recipe.Id);
                    entry.SetSelectable(0, recipe.IsOpened);
                }
            }
        }

        private static RecipeCategories RecipeCategory(ICraftingRecipe recipe) =>
            Enum.GetValues<RecipeCategories>()
                .FirstOrDefault(category => recipe.Tags.Contains(category.ToString(), StringComparer.OrdinalIgnoreCase));

        private int CraftableAmount(string recipeId)
        {
            var requirements = _dataProvider?.GetRecipeRequirements(recipeId) ?? [];
            int amount = int.MaxValue;
            foreach (var requirement in requirements.Where(entry => entry.Type == RequirementType.Resource))
                amount = Math.Min(amount, (_inventory?.GetTotalItemAmount(requirement.Id) ?? 0) / requirement.Amount);
            return amount == int.MaxValue ? 0 : amount;
        }

        private void OnRecipeSelected()
        {
            string recipeId = _tree?.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            if (recipeId.Length == 0 || recipeId == _recipeId) return;

            _recipeId = recipeId;
            _item = null;
            _mode = CraftingMode.Create;
            _selectedModifierHash = null;
            ResetChoices();
            RefreshDetails();
        }

        // ---------------------------------------------------------------- details

        private void RefreshDetails()
        {
            ClosePicker();
            RenderItemHeader();
            RenderModifiers();
            RenderRequirements();
            RenderAdditives();
            RenderAction();
        }

        private void RenderItemHeader()
        {
            if (_item != null)
            {
                _itemIcon?.Texture = _item.Icon;
                _itemName?.Text = _item.DisplayName;
                _itemSubtitle?.Text = $"{Localization.Localize(_item.Rarity.ToString())}   +{_item.UpdateLevel} / {_item.MaxUpdateLevel}";
                return;
            }

            string resultId = _recipeId == null ? string.Empty : _dataProvider?.GetRecipeResultItemId(_recipeId) ?? string.Empty;
            _itemIcon?.Texture = resultId.Length > 0 ? _dataProvider?.GetItemIcon(resultId) : null;
            _itemName?.Text = _recipeId == null ? Localization.Localize("UI_Craft_PickRecipe") : Localization.Localize(_recipeId);
            _itemSubtitle?.Text = string.Empty;
        }

        private void RenderModifiers()
        {
            if (_mods == null) return;
            foreach (var child in _mods.GetChildren())
                child.QueueFree();

            bool visible = _item != null;
            _modsHeader?.Visible = visible;
            _mods.Visible = visible;
            if (_item == null) return;

            foreach (var implicitModifier in _item.Implicits)
                _mods.AddChild(new Label { Text = Localization.Format(implicitModifier), ThemeTypeVariation = "DimLabel" });

            foreach (var modifier in _item.Modifiers)
                _mods.AddChild(_mode == CraftingMode.Recraft ? RerollableRow(modifier) : new Label { Text = Localization.Format(modifier) });
        }

        /// <summary>In Recraft the additional lines become pickable — the chosen one gets rerolled.</summary>
        private Button RerollableRow(IModifier modifier)
        {
            int hash = modifier.GetHashCode();
            var row = new Button
            {
                Text = (_selectedModifierHash == hash ? "» " : string.Empty) + Localization.Format(modifier),
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
            };
            row.Pressed += () =>
            {
                _selectedModifierHash = hash;
                RefreshDetails();
            };
            return row;
        }

        private void RenderRequirements()
        {
            if (_requirements == null) return;
            foreach (var child in _requirements.GetChildren())
                child.QueueFree();

            foreach (var requirement in CurrentRequirements())
            {
                if (requirement.Type == RequirementType.ResourceCategory)
                    _requirements.AddChild(CategoryRow(requirement));
                else
                    _requirements.AddChild(StaticRow(requirement));
            }
        }

        private Control StaticRow(Core.Interfaces.IRequirement requirement)
        {
            int have = requirement.Type == RequirementType.MasteryLevel
                ? _mastery?.CurrentLevel ?? 0
                : _inventory?.GetTotalItemAmount(requirement.Id) ?? 0;

            var row = new HBoxContainer();
            row.AddChild(new Label { Text = Localization.Localize(requirement.Id), SizeFlagsHorizontal = SizeFlags.ExpandFill });
            row.AddChild(new Label
            {
                Text = $"{have} / {requirement.Amount}",
                ThemeTypeVariation = have >= requirement.Amount ? null : "DimLabel",
            });
            return row;
        }

        /// <summary>A category requirement is a slot: click → pick a concrete resource carrying the tag.</summary>
        private Control CategoryRow(Core.Interfaces.IRequirement requirement)
        {
            bool chosen = _categoryChoices.TryGetValue(requirement.Id, out string? resourceId);
            int have = chosen ? _inventory?.GetTotalItemAmount(resourceId!) ?? 0 : 0;

            var row = new Button
            {
                Text = chosen
                    ? $"{Localization.Localize(resourceId!)}   {have} / {requirement.Amount}"
                    : $"{Localization.Localize("UI_Craft_Choose")}: {Localization.Localize(requirement.Id)}",
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
            };
            row.Pressed += () => OpenPicker(
                Localization.Localize(requirement.Id),
                CategoryResourceIds(requirement.Id),
                picked =>
                {
                    _categoryChoices[requirement.Id] = picked;
                    RefreshDetails();
                });
            return row;
        }

        private List<string> CategoryResourceIds(string categoryTag) =>
            (_inventory?.GetAllItemIdsWithTag(categoryTag) ?? [])
            .Distinct()
            .Where(id => !_categoryChoices.ContainsValue(id))
            .ToList();

        private void RenderAdditives()
        {
            if (_additives == null) return;
            foreach (var child in _additives.GetChildren())
                child.QueueFree();

            // Nothing to offer while the CraftingAdditives catalog is empty — hide the block entirely.
            bool visible = (_additiveProvider?.KnownAdditiveIds.Count ?? 0) > 0 && (_item != null || _recipeId != null);
            _additivesHeader?.Visible = visible;
            _additives.Visible = visible;
            if (!visible) return;

            for (int slot = 0; slot < AdditiveSlots; slot++)
            {
                int index = slot;
                string? choice = _additiveChoices[slot];
                var button = new Button
                {
                    Text = choice == null ? "+" : Localization.Localize(choice),
                    CustomMinimumSize = new Vector2(120, 40),
                    FocusMode = FocusModeEnum.None,
                };
                button.Pressed += () =>
                {
                    if (_additiveChoices[index] != null)
                    {
                        _additiveChoices[index] = null; // click on a filled slot empties it
                        RefreshDetails();
                        return;
                    }

                    OpenPicker(Localization.Localize("UI_Craft_Additives"), AvailableAdditiveIds(), picked =>
                    {
                        _additiveChoices[index] = picked;
                        RefreshDetails();
                    });
                };
                _additives.AddChild(button);
            }
        }

        private List<string> AvailableAdditiveIds() =>
            (_additiveProvider?.KnownAdditiveIds ?? [])
            .Where(id => (_inventory?.GetTotalItemAmount(id) ?? 0) > 0 && !_additiveChoices.Contains(id))
            .ToList();

        private void RenderAction()
        {
            if (_actionButton == null) return;

            _actionButton.Text = Localization.Localize(_mode switch
            {
                CraftingMode.Upgrade => "UI_Craft_Upgrade",
                CraftingMode.Recraft => "UI_Craft_Reroll",
                CraftingMode.Ascend => "UI_Craft_Ascend",
                _ => "UI_Craft_Create",
            });
            _actionButton.Disabled = !CanExecute();
        }

        // ---------------------------------------------------------------- costs and execution

        private List<Core.Interfaces.IRequirement> CurrentRequirements()
        {
            if (_mode == CraftingMode.Create)
                return _recipeId == null ? [] : (_dataProvider?.GetRecipeRequirements(_recipeId) ?? []).ToList();
            if (_item == null || _upgrader == null || _ascender == null) return [];

            var category = _item.EquipmentPiece.ConvertEquipmentPartToCategory();
            return _mode switch
            {
                CraftingMode.Upgrade => _upgrader.GetUpgradeResourceCost(_item.Rarity, category),
                CraftingMode.Recraft => _upgrader.GetRecraftResourceCost(_item.Rarity, category),
                CraftingMode.Ascend => _ascender.GetAscendResourceCost(category),
                _ => [],
            };
        }

        /// <summary>Everything the operation consumes: fixed resources, chosen category resources, additives.</summary>
        private Dictionary<string, int> BuildCost()
        {
            var cost = new Dictionary<string, int>();
            foreach (var requirement in CurrentRequirements())
            {
                switch (requirement.Type)
                {
                    case RequirementType.Resource:
                        cost[requirement.Id] = cost.GetValueOrDefault(requirement.Id) + requirement.Amount;
                        break;
                    case RequirementType.ResourceCategory when _categoryChoices.TryGetValue(requirement.Id, out string? chosen):
                        cost[chosen] = cost.GetValueOrDefault(chosen) + requirement.Amount;
                        break;
                }
            }

            foreach (string? additive in _additiveChoices)
                if (additive != null)
                    cost[additive] = cost.GetValueOrDefault(additive) + 1;
            return cost;
        }

        private bool CanExecute()
        {
            if (_inventory == null) return false;

            bool costCovered = BuildCost().All(pair => _inventory.GetTotalItemAmount(pair.Key) >= pair.Value);
            return _mode switch
            {
                CraftingMode.Create => _recipeId != null && costCovered && MasteryAllows() && AllCategoriesChosen(),
                CraftingMode.Upgrade => _item != null && _item.UpdateLevel < _item.MaxUpdateLevel && costCovered,
                CraftingMode.Recraft => _item != null && _selectedModifierHash != null && costCovered,
                CraftingMode.Ascend => _item != null && _ascender?.CanAscend(_item) == true && costCovered,
                _ => false,
            };
        }

        private bool MasteryAllows() =>
            CurrentRequirements().Where(requirement => requirement.Type == RequirementType.MasteryLevel)
                .All(requirement => (_mastery?.CurrentLevel ?? 0) >= requirement.Amount);

        private bool AllCategoriesChosen() =>
            CurrentRequirements().Where(requirement => requirement.Type == RequirementType.ResourceCategory)
                .All(requirement => _categoryChoices.ContainsKey(requirement.Id));

        private async void OnActionPressed()
        {
            try
            {
                if (_messageBus == null || !CanExecute()) return;
                var cost = BuildCost();

                switch (_mode)
                {
                    case CraftingMode.Create:
                        await _messageBus.SendRequest<CreateEquipItemRequest, IEquipItem?>(new(_recipeId!, cost));
                        break;
                    case CraftingMode.Upgrade:
                        await _messageBus.SendRequest<UpgradeEquipItemRequest, ItemUpgradeResult>(new(_item!.InstanceId, cost));
                        break;
                    case CraftingMode.Recraft:
                        await _messageBus.SendRequest<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>(
                            new(_item!.InstanceId, _selectedModifierHash!.Value, cost));
                        _selectedModifierHash = null;
                        break;
                    case CraftingMode.Ascend:
                        await _messageBus.SendRequest<AscendEquipItemRequest, AscensionResult>(new(_item!.InstanceId));
                        break;
                }

                RefreshDetails();
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Crafting action failed", exception, this);
                GD.Print($"Crafting action failed: {exception.Message}");
            }
        }

        // ---------------------------------------------------------------- picker

        private void OpenPicker(string title, List<string> ids, Action<string> onPicked)
        {
            if (_picker == null || _pickerList == null) return;

            _pickerCallback = onPicked;
            _pickerTitle?.Text = title;
            _pickerList.Clear();
            _pickerIds.Clear();

            foreach (string id in ids)
            {
                _pickerIds.Add(id);
                _pickerList.AddItem($"{Localization.Localize(id)}   ({_inventory?.GetTotalItemAmount(id) ?? 0})", _dataProvider?.GetItemIcon(id));
            }

            _picker.Visible = true;
        }

        private void OnPickerActivated(long index)
        {
            if (index < 0 || index >= _pickerIds.Count) return;
            var callback = _pickerCallback;
            string picked = _pickerIds[(int)index];
            ClosePicker();
            callback?.Invoke(picked);
        }

        private void ClosePicker()
        {
            _pickerCallback = null;
            if (_picker != null) _picker.Visible = false;
        }

        // ---------------------------------------------------------------- misc

        private void ResetChoices()
        {
            _categoryChoices.Clear();
            Array.Clear(_additiveChoices);
        }

        private void OnItemAmountChanged(string itemId, int amount)
        {
            BuildTree();
            RefreshDetails();
        }
    }
}
