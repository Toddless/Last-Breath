namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Data;
    using Core.Enums;
    using Core.Results;
    using Core.Modifiers;
    using Core.Constants;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.MessageBus;
    using System.Collections.Generic;
    using Core.Interfaces.MessageBus.Requests;

    public partial class CraftingWindow : Control, IWindow
    {
        private const string UID = "uid://betq124kfglyy";

        [Export] private VBoxContainer? _requirements, _recipeContainer;
        [Export] private HBoxContainer? _buttons;
        [Export] private ItemUi? _itemUi;

        [Export] private Recipes? _recipes;
        [Export] private CraftingRequirementsUi? _resourcesUi;
        [Export] private Button? _canBeCrafted;

        private IItemDataProvider? _dataProvider;
        private IGameMessageBus? _messageBus;
        private IGameServiceProvider? _provider;

        private CraftingMode _craftingMode;
        private ActionButton? _actionButton;
        private string? _recipeId;
        private IEquipItem? _equipItem;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _recipes?.RecipeSelected += SetRecipe;
            _resourcesUi?.ItemCanBeCrafted += OnItemCanBeCrafted;
            _canBeCrafted?.Pressed += OnCraftPressed;
            _itemUi?.ModifierSelected += OnModifierSelectedAsync;
        }

        private void OnCraftPressed()
        {
            switch (_craftingMode)
            {
                case CraftingMode.Create:
                    if (string.IsNullOrWhiteSpace(_recipeId)) return;
                    CreateItemAsync(_recipeId);
                    break;
                case CraftingMode.Upgrade:
                    if (_equipItem == null) return;
                    UpgradeItemAsync();
                    break;
            }
        }

        private void OnItemCanBeCrafted(bool isItemCanBeCrafted) => _canBeCrafted?.Disabled = !isItemCanBeCrafted;

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed(Settings.Cancel) && IsAlreadyVisible)
            {
                GetParent().RemoveChild(this);
                AcceptEvent();
            }
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _provider = provider;
            _recipes?.InjectServices(provider);
        }

        public void Close() => GetParent().RemoveChild(this);

        public void SetRecipe(string recipeId)
        {
            if (recipeId.Equals(_recipeId, StringComparison.OrdinalIgnoreCase)) return;
            _resourcesUi?.ClearSlots();
            _craftingMode = CraftingMode.Create;
            _recipeId = recipeId;
            var recipe = (ICraftingRecipe)_dataProvider.CopyItem(_recipeId);
            var item = (IEquipItem)_dataProvider.CopyItem(recipe.ResultItemId);

            #region Tests
            var craftingMastery = _provider.GetService<ICraftingMastery>();
            craftingMastery.AddExperience(50000);
            _itemUi?.SetConfiguration(new ItemCreationConfiguration(item, craftingMastery));
            foreach (IRequirement recipeRequirement in recipe.Requirements)
                _resourcesUi?.SetRequirements(recipeRequirement, _provider);
            _resourcesUi?.SetOptional(recipe.OptionalResourceCategories, _provider);

            #endregion
        }


        public async Task SetEquipItemForUpgradeAsync(IEquipItem item)
        {
            ArgumentNullException.ThrowIfNull(_messageBus);
            ArgumentNullException.ThrowIfNull(_provider);
            SetEquipItem(item, new ItemUpgradeConfiguration(item), CraftingMode.Upgrade);
            var requirements = await _messageBus.SendRequest<GetEquipItemUpgradeCostRequest, IEnumerable<IRequirement>>(
                new(item.InstanceId));
            foreach (var req in requirements)
                _resourcesUi?.SetRequirements(req, _provider);
        }

        public async Task SetEquipItemForRecraftAsync(IEquipItem item)
        {
            ArgumentNullException.ThrowIfNull(_messageBus);
            ArgumentNullException.ThrowIfNull(_provider);
            SetEquipItem(item, new ItemUpgradeConfiguration(item), CraftingMode.Recraft, true);
            var requirements = await _messageBus.SendRequest<GetEquipItemRecraftModifierCostRequest, IEnumerable<IRequirement>>(
                new(item.InstanceId));
            foreach (var req in requirements)
                _resourcesUi?.SetRequirements(req, _provider);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async void UpgradeItemAsync()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_messageBus);
                var result = await _messageBus.SendRequest<UpgradeEquipItemRequest, ItemUpgradeResult>(
                    new(_equipItem!.InstanceId, _resourcesUi?.GetRequirements() ?? []));
                _itemUi?.SetConfiguration(new ItemUpgradeConfiguration(_equipItem));
            }
            catch (Exception ex)
            {
                GD.Print($"Failed to update item: {ex.Message}\n {ex.StackTrace}");
                Tracker.TrackException("Failed to upgrade item", ex, this);
            }
        }

        private void SetEquipItem(IEquipItem item, IItemUiConfiguration configuration, CraftingMode craftingMode, bool isModifiersSelectable = false)
        {
            _resourcesUi?.ClearSlots();
            _craftingMode = craftingMode;
            _equipItem = item;
            _itemUi?.SetConfiguration(configuration);
            _itemUi?.SetModifiersSelectable(isModifiersSelectable);
        }

        private async void CreateItemAsync(string id)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_messageBus);
                var item = await _messageBus.SendRequest<CreateEquipItemRequest, IEquipItem?>(new CreateEquipItemRequest(id, _resourcesUi?.GetRequirements() ?? []));
            }
            catch (Exception ex)
            {
                GD.Print($"Failed to create item: {ex.Message}\n {ex.StackTrace}");
                Tracker.TrackException("Failed to create item", ex, this);
            }
        }

        private async void OnModifierSelectedAsync(int identifier, ItemModifierList source)
        {
            try
            {
                if (_craftingMode != CraftingMode.Recraft) return;
                ArgumentNullException.ThrowIfNull(_messageBus);

                var result = await _messageBus.SendRequest<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>(
                    new(_equipItem?.InstanceId ?? string.Empty, identifier, _resourcesUi?.GetRequirements() ?? []));
                if (result.IsSuccess)
                    source.UpdateSelectedItem((Localization.Format(result.Param), result.Param!.GetHashCode()));
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to recraft modifier", ex, this);
            }
        }
    }
}
