namespace LastBreath.Inventory
{
    using System;
    using Core.Data;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
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

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            if (_craftingButton != null)
                _craftingButton.Pressed += OnCraftingButtonPressed;
        }

        public override void _ExitTree()
        {
            if (Bag != null)
            {
                Bag.DetachSlots();
                Bag.ItemInteraction -= OnItemInteraction;
            }

            if (Equipment is { } equipment) equipment.EquipmentChanged -= OnEquipmentChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _inventory = provider.GetService<IInventory>();
            _messageBus = provider.GetService<IGameMessageBus>();
            _playerAccessor = provider.GetService<IPlayerAccessor>();

            if (Bag != null && _inventoryGrid != null)
            {
                Bag.AttachSlots(_inventoryGrid);
                Bag.ItemInteraction += OnItemInteraction;
            }

            if (Equipment is { } equipment) equipment.EquipmentChanged += OnEquipmentChanged;
            RenderEquipment();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Close() => QueueFree();

        private Inventory? Bag => _inventory as Inventory;

        private Core.Components.IEquipmentComponent? Equipment => _playerAccessor?.Player?.EquipmentComponent;

        private void OnCraftingButtonPressed() => _messageBus?.PublishMessageAsync(new OpenCraftingWindowMessage(string.Empty));

        private void OnEquipmentChanged(EquipmentPiece piece, IEquipItem? item) => RenderEquipment();

        /// <summary>Right-click in the bag wears the piece; whatever it replaced goes back in.</summary>
        private void OnItemInteraction(IItem item, MouseInteractions interaction)
        {
            if (interaction != MouseInteractions.RightClick || item is not IEquipItem equipItem) return;
            if (Equipment is not { } equipment || _inventory == null) return;

            if (!equipment.TryEquip(equipItem, out var replaced)) return;
            _inventory.RemoveItemByInstanceId(equipItem.InstanceId);
            if (replaced != null) _inventory.TryAddItem(replaced);
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
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = piece.ToString(),
                ThemeTypeVariation = "DimLabel",
                CustomMinimumSize = new Vector2(80, 0),
            });
            row.AddChild(new Label
            {
                Text = equipped?.DisplayName ?? "—",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClipContents = true,
            });

            if (equipped == null) return row;

            var unequip = new Button { Text = "×", CustomMinimumSize = new Vector2(28, 28), FocusMode = FocusModeEnum.None };
            unequip.Pressed += () => OnUnequipPressed(piece);
            row.AddChild(unequip);
            return row;
        }
    }
}
