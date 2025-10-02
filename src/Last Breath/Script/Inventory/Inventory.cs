namespace LastBreath.Script.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Godot;
    using Utilities;

    public class Inventory 
    {
        private readonly Dictionary<string, IItem> _itemInstances = [];
        protected List<IInventorySlot> Slots { get; } = [];

        public event Action<string, MouseButtonPressed, IInventory>? ItemSlotClicked;
        public event Action<string, string, int, int>? InventoryFull;
        public event Action<string>? NotEnougthItems;
        public event Action<string, int>? ItemAmountChanges;

        public void Initialize(int amount, GridContainer? container)
        {
            if (container != null)
            {
                for (int i = 0; i < amount; i++)
                {
                    InventorySlot inventorySlot = InventorySlot.Initialize().Instantiate<InventorySlot>();
                    inventorySlot.ItemDeleted += OnDeleteRequested;
                    container.AddChild(inventorySlot);
                    Slots.Add(inventorySlot);
                }
            }
        }

        public ItemInstance? GetCurrentItemByInstanceId(string instanceId) => Slots.FirstOrDefault(x => x.CurrentItem?.InstanceId == instanceId)?.CurrentItem;
        public List<string> GetAllItemIdsWithTag(string tag) => [.. _itemInstances.Values.Where(x => x.HasTag(tag)).Select(x => x.Id)];
        public IItem? GetItemInstance(string instanceId) => _itemInstances.GetValueOrDefault(instanceId);

        public int GetTotalItemAmount(string itemId) => Slots.Where(x => x.CurrentItem != null && x.CurrentItem.ItemId == itemId).Sum(x => x.Quantity);

        /// <summary>
        /// Add the stack of the item to the existing instance. <see langword="true"/> if the stack was added, <see langword="false"/> if the instance was not found.
        /// </summary>
        /// <param name="itemId">Meaningful Id. For example "Crafting_Resource_Diamond"</param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool TryAddItemStacks(string itemId, int amount = 1)
        {
            var itemInstance = _itemInstances.Values.FirstOrDefault(x => x.Id == itemId);
            if (itemInstance == null) return false;
            FitItemsInSlots(itemInstance.Id, itemInstance.InstanceId, amount, itemInstance.MaxStackSize);
            return true;
        }

        public bool TryAddItem(IItem item, int amount = 1)
        {
            if (amount <= 0) return false;

            if (!_itemInstances.ContainsKey(item.InstanceId))
                _itemInstances[item.InstanceId] = item;

            FitItemsInSlots(item.Id, item.InstanceId, amount, item.MaxStackSize);

            ItemAmountChanges?.Invoke(item.Id, GetTotalItemAmount(item.Id));

            return true;
        }

        /// <summary>
        /// Use to return item instance in inventory.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="amount"></param>
        /// <returns><see langword="true"/> if item was successfully returned, <see langword="false"/> if amount < 0, instance is null, instance id null or empty, or item do not exist in inventory. </returns>
        public bool TryReturnItemInstanceToInventory(ItemInstance? instance, int amount = 1)
        {
            if (amount <= 0 || instance == null || string.IsNullOrWhiteSpace(instance.InstanceId)) return false;
            if (!_itemInstances.TryGetValue(instance.InstanceId, out var value))
            {
                Tracker.TrackNotFound($"Trying to return unknown item. Instance: {instance.InstanceId}, ItemId: {instance.ItemId}", this);
                return false;
            }

            FitItemsInSlots(instance.ItemId, instance.InstanceId, amount, value.MaxStackSize);
            return true;
        }

        public void RemoveItemById(string itemId, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(itemId)) return;

            int remainToDelete = amount;

            var slotsWithItem = Slots.Where(x => x.CurrentItem?.ItemId == itemId).OrderBy(x => x.Quantity);

            if (slotsWithItem == null)
            {
                Tracker.TrackError($"Trying to remove non existent item from inventory: {itemId}", this);
                return;
            }

            foreach (var slot in slotsWithItem)
            {
                if (remainToDelete <= 0) break;
                int canRemove = Mathf.Min(remainToDelete, slot.Quantity);
                if (slot.TryRemoveItemStacks(canRemove))
                    remainToDelete -= canRemove;
                else
                    Tracker.TrackError("");
            }
            if (remainToDelete > 0) NotEnougthItems?.Invoke(itemId);

            ItemAmountChanges?.Invoke(itemId, GetTotalItemAmount(itemId));
        }

        public void RemoveItemByInstanceId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) return;

            var slots = Slots.Where(x => x.CurrentItem?.InstanceId == instanceId);
            if (slots != null)
                foreach (var slot in slots)
                    slot.ClearSlot(isDeleted: true);
            else
                if (_itemInstances.ContainsKey(instanceId))
                _itemInstances.Remove(instanceId);
        }

        public void Clear()
        {
            Slots.ForEach(slot => slot.ClearSlot());
            _itemInstances.Clear();
        }

        protected void OnDeleteRequested(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            if (!_itemInstances.TryGetValue(itemId, out var toRemove))
            {
                Tracker.TrackNotFound($"Item with id: {itemId}", this);
                return;
            }
            if (!Slots.Any(x => x.CurrentItem?.InstanceId == itemId))
            {
                _itemInstances.Remove(toRemove.InstanceId);
                ItemAmountChanges?.Invoke(toRemove.Id, GetTotalItemAmount(itemId));
            }
        }

        private void FitItemsInSlots(string itemId, string instanceId, int amount, int maxStackSize)
        {
            var remaining = amount;
            var slotsWithSameItem = Slots.Where(x => x.CurrentItem?.InstanceId == instanceId && x.CurrentItem?.ItemId == itemId);
            foreach (var slot in slotsWithSameItem)
            {
                if (remaining <= 0) break;
                slot.TryAddStacks(amount, out int left);
                remaining = left;
            }

            // add what left in empty slots
            while (remaining > 0)
            {
                var emptySlot = Slots.FirstOrDefault(x => x.CurrentItem == null);
                if (emptySlot == null)
                {
                    // send info about item
                    InventoryFull?.Invoke(itemId, instanceId, amount, maxStackSize);
                    return;
                }

                int toAdd = Mathf.Min(maxStackSize, remaining);
                emptySlot.SetItem(new(itemId, instanceId, maxStackSize), toAdd);
                remaining -= toAdd;
            }
        }
    }
}
