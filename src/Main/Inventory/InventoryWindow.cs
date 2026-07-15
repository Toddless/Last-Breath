namespace LastBreath.Inventory
{
    using System;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Services;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The bag (slots owned by the Inventory service, borrowed into this window's grid) plus the
    /// equipment column. Right-click an equippable item in the bag to wear it; the unequip button
    /// on a row returns the piece to the bag. No drag-and-drop between the two yet.
    /// </summary>
    public partial class InventoryWindow : Panel, IWindow
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private GridContainer? _inventoryGrid;
        [Export] private VBoxContainer? _equipment;

        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private IPlayerAccessor? _playerAccessor;
        private IUiElementsManager? _uiElementsManager;
        private bool _destroyMode;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            _craftingButton?.Pressed += OnCraftingButtonPressed;
            _allStatsButton?.Pressed += () => _uiElementsManager?.ToggleWindow(typeof(UI.CharacterWindow));
            _sortButton?.Pressed += () => Bag?.SortBag();
            _destroyButton?.ToggleMode = true;
            _destroyButton?.Toggled += pressed => _destroyMode = pressed;
        }

        public override void _ExitTree()
        {
            if (Bag != null)
            {
                Bag.DetachSlots();
                Bag.ItemInteraction -= OnItemInteraction;
                Bag.EquipmentDroppedIntoBag -= OnEquipmentDroppedIntoBag;
            }

            if (Equipment is { } equipment) equipment.EquipmentChanged -= OnEquipmentChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _inventory = provider.GetService<IInventory>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _playerAccessor = provider.GetService<IPlayerAccessor>();
            _uiElementsManager = provider.GetService<IUiElementsManager>();

            if (Bag != null && _inventoryGrid != null)
            {
                Bag.AttachSlots(_inventoryGrid);
                Bag.ItemInteraction += OnItemInteraction;
                Bag.EquipmentDroppedIntoBag += OnEquipmentDroppedIntoBag;
            }

            if (Equipment is { } equipment) equipment.EquipmentChanged += OnEquipmentChanged;
            RenderEquipment();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Close() => QueueFree();

        private Inventory? Bag => _inventory as Inventory;

        private IEquipmentComponent? Equipment => _playerAccessor?.Player?.EquipmentComponent;

        private void OnCraftingButtonPressed() => _messageBus?.PublishMessageAsync(new OpenCraftingWindowMessage(string.Empty));

        private void OnEquipmentChanged(EquipmentPiece piece, IEquipItem? item) => RenderEquipment();

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

        private void EquipFromBag(IEquipItem equipItem)
        {
            if (Equipment is not { } equipment || _inventory == null) return;

            if (!equipment.TryEquip(equipItem, out var replaced)) return;
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

        private bool CanEquipFromBag(EquipmentPiece piece, string instanceId) =>
            Bag?.GetItem<IItem>(instanceId) is IEquipItem item && item.EquipmentPiece == piece;

        private void EquipInstanceFromBag(string instanceId)
        {
            if (Bag?.GetItem<IItem>(instanceId) is IEquipItem item) EquipFromBag(item);
        }

        private void OnUnequipPressed(EquipmentPiece piece)
        {
            if (Equipment is not { } equipment || _inventory == null) return;
            if (_inventory.GetAvailableCapacity() == 0) return;
            if (equipment.TryUnequip(piece, out var removed) && removed != null)
                _inventory.TryAddItem(removed);
        }

        private void RenderEquipment()
        {
            if (_equipment == null) return;

            foreach (var child in _equipment.GetChildren())
                child.QueueFree();

            foreach (EquipmentPiece piece in Enum.GetValues<EquipmentPiece>())
                _equipment.AddChild(BuildEquipmentRow(piece));
        }

        private HBoxContainer BuildEquipmentRow(EquipmentPiece piece)
        {
            var equipped = Equipment?.GetEquipped(piece);
            var row = new EquipmentRow();
            row.Setup(piece, equipped, instanceId => CanEquipFromBag(piece, instanceId), EquipInstanceFromBag);
            row.AddChild(new Label { Text = piece.ToString(), ThemeTypeVariation = "DimLabel", CustomMinimumSize = new Vector2(80, 0), });
            var name = new Label
            {
                Text = equipped?.DisplayName ?? "—",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipContents = true,
                // Pass, not Stop: the label feeds the hover tooltip but must let drag&drop
                // bubble to the row — Stop would swallow drags over the item name.
                MouseFilter = MouseFilterEnum.Pass,
            };
            row.AddChild(name);

            if (equipped == null) return row;
            name.AddThemeColorOverride("font_color", Color.FromHtml(Core.Localization.TextPalette.RarityColor(equipped.Rarity)));
            HoverTooltip.Attach(name, () => ShowEquippedTooltip(piece));

            var unequip = new Button { Text = "×", CustomMinimumSize = new Vector2(28, 28), FocusMode = FocusModeEnum.None };
            unequip.Pressed += () => OnUnequipPressed(piece);
            row.AddChild(unequip);
            return row;
        }

        /// <summary>Resolved at hover time: the row may outlive the piece it was built for.</summary>
        private IPopup? ShowEquippedTooltip(EquipmentPiece piece)
        {
            if (Equipment?.GetEquipped(piece) is not { } item) return null;
            var popup = _uiElementsManager?.ShowPopup(typeof(UI.ItemTooltipPopup)) as UI.ItemTooltipPopup;
            popup?.ShowItem(item);
            return popup;
        }
    }
}
