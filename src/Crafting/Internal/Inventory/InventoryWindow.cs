namespace Crafting.Internal.Inventory
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.UI;
    using Godot;

    internal partial class InventoryWindow : Control, IWindow
    {
        private const string UID = "uid://2kxodijod11l";

        [Export] private GridContainer? _inventoryGrid;
        private List<IInventorySlot> Slots { get; } = [];

        private IInventory? _inventory;
        private IGameServiceProvider? _provider;
        private IItemDataProvider? _dataProvider;
        private IGameMessageBus? _messageBus;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            for (int i = 0; i < _inventory?.InventoryCapacity; i++)
            {
                var slot = InventorySlot.Initialize().Instantiate<InventorySlot>();
                slot.GetItemIcon = GetIconForSlot;
                slot.ItemInteraction += OnSlotInteraction;
                _inventoryGrid?.AddChild(slot);
                Slots.Add(slot);
            }

            _inventory?.ItemAmountChanges += OnItemAmountChanged;

            // for tests
            var dataProvider = _provider?.GetService<IItemDataProvider>();
            foreach (var resource in dataProvider?.GetAllResources() ?? [])
                _inventory?.TryAddItem(resource, 100);
        }

        public override void _ExitTree()
        {
            _inventory?.ItemAmountChanges -= OnItemAmountChanged;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _inventory = provider.GetService<IInventory>();
            _provider = provider;
            _dataProvider = provider.GetService<IItemDataProvider>();
            _messageBus = provider.GetService<IGameMessageBus>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
        public void Close() => GetParent().RemoveChild(this);

        // Called by the inventory whenever a stack's quantity changes.
        // instanceId identifies which item changed; newQuantity == 0 means it was removed.
        private void OnItemAmountChanged(string instanceId, int newQuantity)
        {
            if (newQuantity <= 0)
            {
                ClearSlotForInstance(instanceId);
                return;
            }

            var existing = FindSlotWithAnItemByInstanceId(instanceId);
            if (existing != null)
            {
                existing.Quantity = newQuantity;
                return;
            }

            // New item entering the inventory — place it in the first empty slot.
            var emptySlot = Slots.FirstOrDefault(slot => slot.CurrentItem == null);
            if (emptySlot == null) return;

            var item = _inventory?.GetItem<IItem>(instanceId);
            if (item == null) return;

            emptySlot.SetItem(new ItemInstance(item.Id, instanceId, item.MaxStackSize), newQuantity);
        }

        private void OnSlotInteraction(IInventorySlot slot, MouseInteractions interaction)
        {
            if (slot.CurrentItem == null || _messageBus == null) return;
        }

        private Texture2D? GetIconForSlot(string instanceId)
        {
            var item = _inventory?.GetItem<IItem>(instanceId);
            return item == null ? null : _dataProvider?.GetItemIcon(item.Id);
        }

        // Linear scan over 216 slots is acceptable; avoids the extra bookkeeping
        // required to keep a separate instanceId→slot dictionary in sync with
        // the internal drag-and-drop rearrangement in Slot.
        private IInventorySlot? FindSlotWithAnItemByInstanceId(string instanceId) =>
            Slots.FirstOrDefault(s => s.CurrentItem?.InstanceId == instanceId);

        private void ClearSlotForInstance(string instanceId) => FindSlotWithAnItemByInstanceId(instanceId)?.ClearSlot();
    }
}
