namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Results;
    using Core.Constants;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Requests;

    public partial class CraftingWindow : DraggableWindow, IInitializable, IClosable, IRequireServices
    {
        private const string UID = "uid://betq124kfglyy";

        [Export] private Button? _close, _add;
        [Export] private RichTextLabel? _description;
        [Export] private Control? _skill;
        [Export] private Label? _itemName, _itemUpgradeLevel;
        [Export] private VBoxContainer? _requirements, _additionalStats, _baseStats, _recipeContainer;
        [Export] private HBoxContainer? _buttons;
        [Export] private TextureRect? _itemIcon;

        private Recipes? _recipes;
        private IItemDataProvider? _dataProvider;
        private IMediator? _mediator;
        private IUiElementProvider? _uiElementProvider;
        private IUIResourcesProvider? _uiResourcesProvider;

        private CraftingMode _craftingMode;
        private ActionButton? _actionButton;
        private Dictionary<string, int> _usedResources = [];
        private Dictionary<string, Texture2D> _iconCache = [];
        private string? _recipeId;
        private IEquipItem? _equpItem;
        public event Action? Close;

        public override void _Ready()
        {
            _close.Pressed += () => Close?.Invoke();
            _add.Pressed += OnAddPressedAsync;
            if (_uiElementProvider != null)
            {
                _recipes = _uiElementProvider.CreateRequireServices<Recipes>();
                _recipeContainer?.AddChild(_recipes);
                _recipes.RecipeSelected += SetRecipe;
            }

            // TODO: Update this window after reopening
        }

        public override void _EnterTree()
        {
            if (_mediator != null) _mediator.UpdateUi += UpdateRequiredResourcesAsync;
            if (DragArea != null) DragArea.GuiInput += DragWindow;
            UpdateRequiredResourcesAsync();
        }

        public override void _ExitTree()
        {
            if (_mediator != null) _mediator.UpdateUi -= UpdateRequiredResourcesAsync;
            if (DragArea != null) DragArea.GuiInput -= DragWindow;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel) && Visible)
            {
                GetParent().RemoveChild(this);
                AcceptEvent();
            }
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _mediator = provider.GetService<IMediator>();
            _uiElementProvider = provider.GetService<IUiElementProvider>();
            _uiResourcesProvider = provider.GetService<IUIResourcesProvider>();
            _mediator.UpdateUi += UpdateRequiredResourcesAsync;
        }

        public void SetRecipe(string recipeId)
        {
            if (recipeId.Equals(_recipeId, StringComparison.OrdinalIgnoreCase)) return;
            RemoveOldData();
            _craftingMode = CraftingMode.Create;
            _recipeId = recipeId;

            SetMainResourceRequirementsAsync(_dataProvider?.GetRecipeRequirements(_recipeId) ?? []);
            SetPossibleBaseStats();
            SetPossibleModifiers();
            SetDisplayableData(recipeId);
            CreateActionButtons();
        }

        public void SetItem(IEquipItem? item)
        {
            if (item?.InstanceId.Equals(_equpItem?.InstanceId, StringComparison.OrdinalIgnoreCase) ?? true) return;
            RemoveOldData();
            _craftingMode = CraftingMode.Upgrade;
            _equpItem = item;

            SetBaseStats();
            SetAdditionalModifiers();
            SetSkill();
            SetDisplayableData(_equpItem?.Id ?? string.Empty);
            CreateActionButtons();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);


        private void CreateActionButtons()
        {
            _buttons?.AddChild(CreateaActionButton(SetButtonName(), PerformActionOnItem));
            if (_craftingMode != CraftingMode.Create)
            {
                var buttonGroup = new ButtonGroup();
                _buttons?.AddChild(CreateToggleUpgradeButton("N", ItemUpgradeMode.Normal, buttonGroup));
                _buttons?.AddChild(CreateToggleUpgradeButton("D", ItemUpgradeMode.Double, buttonGroup));
                _buttons?.AddChild(CreateToggleUpgradeButton("L", ItemUpgradeMode.Lucky, buttonGroup));
                var recraftBtn = new ActionButton();
                recraftBtn.SetupToggleButton("R", OnRecraftModeToggledAsync, buttonGroup);
                _buttons?.AddChild(recraftBtn);
                var ascendBtn = new ActionButton();
                ascendBtn.SetupToggleButton("A", OnAscendModeToggled, buttonGroup);
                _buttons?.AddChild(ascendBtn);
            }
        }


        private ActionButton CreateaActionButton(string name, Action onPressed)
        {
            var btn = new ActionButton();
            btn.SetupNormalButton(name, onPressed);
            _actionButton = btn;
            UpdateActionButtonStateAsync();
            return btn;
        }

        private ActionButton CreateToggleUpgradeButton(string name, ItemUpgradeMode mode, ButtonGroup group)
        {
            var btn = new ActionButton();
            btn.SetupToggleButton(name, isToggles => OnUpgradeModeToggledAsync(isToggles, mode), group);
            return btn;
        }

        private void PerformActionOnItem()
        {
            try
            {
                switch (_craftingMode)
                {
                    case CraftingMode.Upgrade:
                        UpgradeEquipItemAsync();
                        break;
                    case CraftingMode.Ascend:
                        AscendEquipItem();
                        break;
                    case CraftingMode.Recraft:
                        return;
                    case CraftingMode.Create:
                        CreateItemAsync(_recipeId ?? string.Empty);
                        break;
                }

                UpdateActionButtonStateAsync();
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Perform action went wrong. Crafint mode: {_craftingMode}", ex, this);
            }
        }

        private void AscendEquipItem()
        {
        }

        private async void CreateItemAsync(string id)
        {
            ArgumentNullException.ThrowIfNull(_mediator);
            var item = await _mediator.Send<CreateEquipItemRequest, IEquipItem?>(new(id, _usedResources));
            if (item != null)
            {
                _mediator?.PublishAsync(new ItemCreatedEvent(item));
            }
        }

        private async void UpgradeEquipItemAsync()
        {
            ArgumentNullException.ThrowIfNull(_mediator);
            var result =
                await _mediator.Send<UpgradeEquipItemRequest, ItemUpgradeResult>(
                    new(_equpItem?.InstanceId ?? string.Empty, _usedResources));
            switch (result)
            {
                case ItemUpgradeResult.Success:
                    break;
                case ItemUpgradeResult.Failure:
                    break;
            }

            UpdateItemStats();
        }

        private async void UpdateActionButtonStateAsync() =>
            _actionButton?.UpdateButtonState(await AllRequirementsMetAsync());

        private string SetButtonName()
        {
            return _craftingMode switch
            {
                CraftingMode.Create => Localizator.Localize("CraftingCreateButton"),
                CraftingMode.Upgrade => Localizator.Localize("CraftingUpgradeButton"),
                CraftingMode.Ascend => Localizator.Localize("CraftingAscendButton"),
                _ => string.Empty
            };
        }

        private void UpdateItemStats()
        {
            var baseModifiersList = _baseStats?.GetChildren().Cast<ItemModifierList>().FirstOrDefault();
            var additionalModifiersList = _additionalStats?.GetChildren().Cast<ItemModifierList>().FirstOrDefault();

            UpdateModifiersInList(_equpItem?.BaseModifiers ?? [], baseModifiersList);
            UpdateModifiersInList(_equpItem?.AdditionalModifiers ?? [], additionalModifiersList);
            if (_itemUpgradeLevel != null) _itemUpgradeLevel.Text = GetUpdateLevel();
        }

        private async void OnUpgradeModeToggledAsync(bool isToggled, ItemUpgradeMode mode)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mediator);
                if (!isToggled) return;
                if (_craftingMode != CraftingMode.Upgrade)
                    _craftingMode = CraftingMode.Upgrade;
                var upgradeCost =
                    await _mediator.Send<GetEquipItemUpgradeCostRequest, IEnumerable<IResourceRequirement>>(
                        new(_equpItem?.InstanceId ?? string.Empty, mode));
                SetMainResourceRequirementsAsync(upgradeCost);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to set upgrade mode", ex, this);
            }
        }

        private async void OnRecraftModeToggledAsync(bool isToggled)
        {
            try
            {
                if (isToggled)
                {
                    ArgumentNullException.ThrowIfNull(_mediator);
                    if (_craftingMode != CraftingMode.Recraft)
                        _craftingMode = CraftingMode.Recraft;
                    var recraftCost =
                        await _mediator
                            .Send<GetEquipItemRecraftModifierCostRequest, IEnumerable<IResourceRequirement>>(
                                new(_equpItem?.InstanceId ?? string.Empty));
                    SetMainResourceRequirementsAsync(recraftCost);
                    UpdateAdditionalModifiersSelectable();
                }
                else
                    UpdateAdditionalModifiersSelectable(false);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to set recraft mode", ex, this);
            }
        }


        private void OnAscendModeToggled(bool isToggled)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mediator);
                if (isToggled)
                {
                    if (_craftingMode != CraftingMode.Ascend)
                        _craftingMode = CraftingMode.Ascend;
                    //  var ascendCost = await _systemMediator.Send
                }
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to activate ascend mode", ex, this);
            }
        }

        private async void SetMainResourceRequirementsAsync(IEnumerable<IResourceRequirement> requirements)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mediator);
                _usedResources.Clear();
                FreeChildren(_requirements?.GetChildren() ?? []);
                // maybe i only need to update quantity when we changing upgrade mode
                var result =
                    await _mediator.Send<GetTotalItemAmountRequest, Dictionary<string, int>>(
                        new(requirements.Select(x => x.ResourceId)));

                foreach (var req in requirements)
                {
                    result.TryGetValue(req.ResourceId, out var amount);
                    var reqItem = CreateClickableResource(req.ResourceId, amount, req.Amount);
                    _usedResources.TryAdd(req.ResourceId, req.Amount);
                    _requirements?.AddChild(reqItem);
                }

                UpdateActionButtonStateAsync();
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to set main resource requirements", ex, this);
            }
        }

        private void SetBaseStats()
        {
            try
            {
                _baseStats?.AddChild(CreateItemModifierList(_equpItem?.BaseModifiers ?? [],
                    _uiResourcesProvider?.GetResource("BaseItemStatsSetting") as LabelSettings));
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to create item modifier list", ex, this);
            }
        }

        private void SetAdditionalModifiers()
        {
            try
            {
                var itemModifierList = CreateItemModifierList(_equpItem?.AdditionalModifiers ?? [],
                    _uiResourcesProvider?.GetResource("AdditionalStatsSettings") as LabelSettings);
                itemModifierList.ItemSelected += OnModifierSelectedAsync;
                itemModifierList.TreeExiting += OnItemModifierListFree;

                void OnItemModifierListFree()
                {
                    itemModifierList.ItemSelected -= OnModifierSelectedAsync;
                    itemModifierList.TreeExiting -= OnItemModifierListFree;
                }

                _additionalStats?.AddChild(itemModifierList);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to create item modifier list", ex, this);
            }
        }


        private async void OnModifierSelectedAsync(int hash, ItemModifierList source)
        {
            try
            {
                if (_craftingMode != CraftingMode.Recraft) return;
                ArgumentNullException.ThrowIfNull(_mediator);

                var result =
                    await _mediator.Send<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>(
                        new(_equpItem?.InstanceId ?? string.Empty, hash, _usedResources));
                if (result.IsSuccess)
                    source.UpdateSelectedItem((Localizator.Format(result.Param), result.Param!.GetHashCode()));
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to recraft modifier", ex, this);
            }
        }

        private ItemModifierList CreateItemModifierList(IReadOnlyList<IModifier> modifiers,
            LabelSettings? labelSettings)
        {
            ArgumentNullException.ThrowIfNull(_uiElementProvider);
            var modifiersList = _uiElementProvider.Create<ItemModifierList>();

            var modsWithHash = new List<(string Mod, int Hash)>();
            foreach (var mod in modifiers)
                modsWithHash.Add((Localizator.Format(mod), mod.GetHashCode()));

            modifiersList.AddModifiersToList(modsWithHash, labelSettings);
            modifiersList.SetItemsSelectable(false);

            return modifiersList;
        }

        private void UpdateAdditionalModifiersSelectable(bool isSelectable = true)
        {
            var modifierList = _additionalStats?.GetChildren().Cast<ItemModifierList>().FirstOrDefault() ??
                               throw new MissingMemberException("Item list for additional stats was not created");
            modifierList.SetItemsSelectable(isSelectable);
        }

        private void UpdateModifiersInList(IReadOnlyList<IModifier> modifiers, ItemModifierList? list)
        {
            foreach (var modifier in modifiers)
                list?.UpdateModifierText(modifier.GetHashCode(), Localizator.Format(modifier));
        }

        private void SetSkill()
        {
            var skill = _equpItem?.Skill;
            if (skill == null) return;
            var skillDescription = _uiElementProvider?.Create<SkillDescription>();
            skillDescription?.SetSkillName(skill.DisplayName);
            skillDescription?.SetSkillDescription(skill.Description);
            _skill?.AddChild(skillDescription);
        }

        private async void OnAddPressedAsync()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mediator);
                var takenResources =
                    await _mediator.Send<OpenCraftingItemsWindowRequest, IEnumerable<string>>(new(_usedResources.Keys));
                AddOptionalResourcesAsync(takenResources);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to add optional resource", ex, this);
            }
        }

        private async void AddOptionalResourcesAsync(IEnumerable<string> takenResources)
        {
            ArgumentNullException.ThrowIfNull(_mediator);
            var result =
                await _mediator.Send<GetTotalItemAmountRequest, Dictionary<string, int>>(new(takenResources));

            foreach (var res in takenResources)
            {
                if (_usedResources.TryAdd(res, 1))
                {
                    result.TryGetValue(res, out var amount);
                    var reqIem = CreateClickableResource(res, amount, 1);
                    reqIem.SetRightClickAction(() =>
                    {
                        _usedResources.Remove(res);
                        reqIem.QueueFree();
                    });
                    reqIem.SetClickable(true);
                    _requirements?.AddChild(reqIem);
                }
            }
        }

        private async void UpdateRequiredResourcesAsync()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_requirements);
                ArgumentNullException.ThrowIfNull(_mediator);
                var clickableResources = _requirements.GetChildren().Cast<ClickableResource>() ?? [];
                var result =
                    await _mediator.Send<GetTotalItemAmountRequest, Dictionary<string, int>>(
                        new(clickableResources.Select(x => x.GetResourceId())));
                foreach (var resource in clickableResources)
                {
                    var resourceId = resource.GetResourceId();
                    result.TryGetValue(resourceId, out var amount);
                    _usedResources.TryGetValue(resourceId, out var need);
                    resource.SetText(Localizator.Localize(resourceId), amount, need);
                }
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to update required resources", ex, this);
            }
        }

        private void SetPossibleBaseStats()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_dataProvider);
                var id = _recipeId ?? string.Empty;
                var resultItemId = _dataProvider.GetRecipeResultItemId(id);
                var itemId = _dataProvider.IsItemHasTag(id, "Generic") ? $"{resultItemId}_Generic" : resultItemId;
                var itemBaseStats = _equpItem?.BaseModifiers.ToHashSet() ?? [];
                CreateHoverArea(itemBaseStats, _baseStats);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to set possible base stats", ex, this);
            }
        }

        private void SetPossibleModifiers() => CreateHoverArea([], _additionalStats, true);

        private void CreateHoverArea(HashSet<IModifier> modifier, BoxContainer? areaContainer, bool updatable = false)
        {
            var label = new Label { LabelSettings = new LabelSettings() { FontSize = 14, }, Text = "1-4" };

            var boxContainer = new HBoxContainer() { Alignment = BoxContainer.AlignmentMode.Center, };

            var hover = new HoverableItem();
            if (updatable)
                hover.SetFuncToUpdateModifiers(GetModifiersUsedResources);
            else
                hover.SetModifiersToShow(modifier);

            boxContainer.AddChild(label);
            boxContainer.AddChild(hover);
            areaContainer?.AddChild(boxContainer);
        }

        private IEnumerable<IModifier> GetModifiersUsedResources()
        {
            var possibleModifiers = new List<IModifier>();

            foreach (var res in _usedResources)
                possibleModifiers.AddRange(_dataProvider?.GetResourceModifiers(res.Key) ?? []);

            return possibleModifiers;
        }

        private void SetDisplayableData(string id)
        {
            _itemName.Text = Localizator.Localize(id);
            _description.Text = Localizator.LocalizeDescription(id);
            _itemIcon.Texture = GetChachedIcon(id);
            _itemUpgradeLevel.Text = GetUpdateLevel();
        }

        private string GetUpdateLevel()
        {
            var updateLevel = _equpItem?.UpdateLevel;

            return updateLevel > 0 ? $" +{updateLevel}" : string.Empty;
        }

        private Texture2D? GetChachedIcon(string id)
        {
            if (!_iconCache.TryGetValue(id, out var texture))
            {
                texture = _dataProvider?.GetItemIcon(id);
                if (texture != null) _iconCache[id] = texture;
            }

            return texture;
        }

        private ClickableResource CreateClickableResource(string resourceId, int have, int need)
        {
            var reqItem = ClickableResource.Initialize().Instantiate<ClickableResource>();
            reqItem.SetResourceId(resourceId);
            reqItem.SetIcon(_dataProvider?.GetItemIcon(resourceId));
            reqItem.SetText(Localizator.Localize(resourceId), have, need);
            return reqItem;
        }

        private void FreeChildren(Godot.Collections.Array<Node> children)
        {
            foreach (var child in children)
                child.QueueFree();
        }

        private void RemoveOldData()
        {
            _actionButton = null;
            _recipeId = string.Empty;
            _equpItem = null;
            _usedResources.Clear();
            _iconCache.Clear();
            FreeChildren(_requirements?.GetChildren() ?? []);
            FreeChildren(_buttons?.GetChildren() ?? []);
            FreeChildren(_baseStats?.GetChildren() ?? []);
            FreeChildren(_additionalStats?.GetChildren() ?? []);
            FreeChildren(_skill?.GetChildren() ?? []);
        }

        private async Task<bool> AllRequirementsMetAsync()
        {
            ArgumentNullException.ThrowIfNull(_mediator);
            var result =
                await _mediator
                    .Send<GetTotalItemAmountRequest, Dictionary<string, int>>(new(_usedResources.Keys));
            return _usedResources.Count > 0 && !_usedResources.Any(res => result[res.Key] < res.Value);
        }
    }
}
