namespace LastBreath.Inventory
{
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Services;
    using Core.Views.UI;
    using Godot;
    using SharedUi;
    using UI.Modules;

    /// <summary>
    /// The character inventory screen (Umbral layout "2a"), a thin composer over module scenes:
    /// paperdoll and stats on the left, the filter bar over the borrowed bag grid on the right.
    /// The window keeps the header buttons and every service decision (equip, unequip, destroy,
    /// sort); the modules report clicks and filter changes up and never touch the services.
    /// Right-click an equippable item in the bag to wear it; right-click a filled paperdoll slot
    /// to take the piece off. Drag works both ways.
    /// </summary>
    public partial class InventoryWindow : Control, IWindow
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private Label? _title;
        [Export] private PaperDoll? _paperDoll;
        [Export] private StatsPane? _statsPane;
        [Export] private FilterBar? _filterBar;
        [Export] private BagGrid? _bagGrid;

        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private IPlayerAccessor? _playerAccessor;
        private IUiElementsManager? _uiElementsManager;
        private bool _destroyMode;
        private Inventory? Bag => _inventory as Inventory;
        private IEquipmentComponent? Equipment => _playerAccessor?.Player?.Equipment;

        public override void _Ready()
        {
            _craftingButton?.Pressed += OnCraftingButtonPressed;
            _allStatsButton?.Pressed += () => _uiElementsManager?.ToggleWindow(typeof(UI.CharacterWindow));
            _sortButton?.Pressed += () => Bag?.SortBag();
            _destroyButton?.ToggleMode = true;
            _destroyButton?.Toggled += pressed => _destroyMode = pressed;
            _filterBar?.FilterChanged += ApplyFilters;

            LocalizeStaticLabels();
        }

        public override void _ExitTree()
        {
            _bagGrid?.Detach();
            Bag?.ItemInteraction -= OnItemInteraction;
            Bag?.EquipmentDroppedIntoBag -= OnEquipmentDroppedIntoBag;
            Bag?.ItemAmountChanges -= OnBagChanged;

            Equipment?.EquipmentChanged -= OnEquipmentChanged;
            _playerAccessor?.Player?.Parameters.ParameterChanged -= OnParameterChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _inventory = provider.GetService<IInventory>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _playerAccessor = provider.GetService<IPlayerAccessor>();
            _uiElementsManager = provider.GetService<IUiElementsManager>();
            _statsPane?.SetFormats(provider.GetService<IParameterFormatProvider>());

            if (Bag != null)
            {
                _bagGrid?.Attach(Bag);
                Bag.ItemInteraction += OnItemInteraction;
                Bag.EquipmentDroppedIntoBag += OnEquipmentDroppedIntoBag;
                Bag.ItemAmountChanges += OnBagChanged;
            }

            Equipment?.EquipmentChanged += OnEquipmentChanged;
            _playerAccessor?.Player?.Parameters.ParameterChanged += OnParameterChanged;

            _paperDoll?.Bind(CanEquipFromBag, EquipInstanceFromBag, OnUnequipPressed, ShowEquippedTooltip);
            _paperDoll?.Refresh(Equipment);
            RefreshStats();
            ApplyFilters();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Close() => QueueFree();

        private void LocalizeStaticLabels()
        {
            _title?.Text = Localization.Localize("UI_Inventory");
            _sortButton?.Text = Localization.Localize("UI_Inv_Sort");
            _destroyButton?.Text = Localization.Localize("UI_Inv_Destroy");
            _allStatsButton?.Text = Localization.Localize("UI_Character");
            _craftingButton?.Text = Localization.Localize("UI_Crafting");
        }

        private void OnCraftingButtonPressed() => _messageBus?.PublishMessageAsync(new OpenCraftingWindowMessage(string.Empty));

        private void OnEquipmentChanged(EquipmentPiece piece, IEquipItem? item)
        {
            _paperDoll?.Refresh(Equipment);
            RefreshStats();
        }

        private void OnBagChanged(string itemId, int total) => ApplyFilters();

        private void OnParameterChanged(EntityParameter parameter, float value) => RefreshStats();

        private void RefreshStats()
        {
            if (_playerAccessor?.Player is { } player) _statsPane?.Refresh(player);
        }

        /// <summary>Right-click in the bag wears the piece; whatever it replaced goes back in.
        /// Destroy mode intercepts the left click and disassembles instead (the crafting pipeline
        /// returns a share of the resources and grants shatter experience).</summary>
        private void OnItemInteraction(IItem item, MouseInteractions interaction)
        {
            if (_destroyMode && interaction == MouseInteractions.LeftClick)
            {
                _messageBus?.PublishMessageAsync(new DestroyItemMessage(item.InstanceId));
                return;
            }

            if (interaction != MouseInteractions.RightClick || item is not IEquipItem equipItem) return;
            EquipFromBag(equipItem);
        }

        private void EquipFromBag(IEquipItem equipItem, EquipmentPiece? targetSlot = null)
        {
            if (Equipment is not { } equipment || _inventory == null) return;

            if (!equipment.TryEquip(equipItem, out var replaced, targetSlot)) return;
            _inventory.RemoveItemByInstanceId(equipItem.InstanceId);
            if (replaced != null) _inventory.TryAddItem(replaced);
        }

        /// <summary>Drag of an equipped piece into a bag slot: unequip into that very slot.</summary>
        private void OnEquipmentDroppedIntoBag(EquipmentPiece piece, IInventorySlot slot)
        {
            if (Equipment is not { } equipment || Bag == null) return;
            if (slot.CurrentItem != null && Bag.GetAvailableCapacity() == 0) return;
            if (!equipment.TryUnequip(piece, out var removed) || removed == null) return;
            Bag.TryAddItemAt(removed, slot);
        }

        private bool CanEquipFromBag(EquipmentPiece slot, string instanceId) =>
            Bag?.GetItem<IItem>(instanceId) is IEquipItem item && item.EquipmentPiece == slot.AcceptedItemPiece();

        /// <summary>A drop on a concrete paperdoll slot equips into THAT slot (either ring accepts a ring).</summary>
        private void EquipInstanceFromBag(EquipmentPiece slot, string instanceId)
        {
            if (Bag?.GetItem<IItem>(instanceId) is IEquipItem item) EquipFromBag(item, slot);
        }

        private void OnUnequipPressed(EquipmentPiece piece)
        {
            if (Equipment is not { } equipment || _inventory == null) return;
            if (_inventory.GetAvailableCapacity() == 0) return;
            if (equipment.TryUnequip(piece, out var removed) && removed != null)
                _inventory.TryAddItem(removed);
        }

        /// <summary>Resolved at hover time: the slot may outlive the piece it was built for.</summary>
        private IPopup? ShowEquippedTooltip(EquipmentPiece piece)
        {
            if (Equipment?.GetEquipped(piece) is not { } item) return null;
            var popup = _uiElementsManager?.ShowPopup(typeof(UI.ItemTooltipPopup)) as UI.ItemTooltipPopup;
            popup?.ShowItem(item);
            return popup;
        }

        private void ApplyFilters() =>
            _bagGrid?.ApplyFilter(_filterBar is { FilterActive: true } ? IsFilteredOut : null);

        /// <summary>Whether the bag slot's item falls out of the current filter (empty slots never dim).</summary>
        private bool IsFilteredOut(IInventorySlot slot)
        {
            var item = slot.CurrentItem == null ? null : _inventory?.GetItem<IItem>(slot.CurrentItem.InstanceId);
            return item != null && _filterBar?.Matches(item) == false;
        }
    }
}
