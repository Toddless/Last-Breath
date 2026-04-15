namespace Crafting.Internal.Inventory
{
    using System;
    using System.Linq;
    using Core.Interfaces.Items;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;
    using System.Collections.Generic;
    using Godot;

    internal class Inventory(IGameMessageBus gameMessageBus) : IInventory
    {
        private readonly Dictionary<string, (IItem Item, int Quantity)> _items = [];

        public int InventoryCapacity => 216;

        public event Action<string, string, int, int>? InventoryFull;
        public event Action<string>? NotEnoughItems;
        public event Action<string, int>? ItemAmountChanges;


        public List<string> GetAllItemIdsWithTag(string tag) =>
            [.. _items.Values.Where(x => x.Item.HasTag(tag)).Select(x => x.Item.Id)];

        public T? GetItem<T>(string instanceId)
            where T : class, IItem => _items.TryGetValue(instanceId, out var item) ? item.Item as T : null;

        public int GetTotalItemAmount(string itemId) =>
            _items.Values.Where(i => i.Item.Id == itemId).Sum(i => i.Quantity);

        public bool TryAddItemStacks(string itemId, int amount = 1)
        {
            // Find the first existing stack of this item that still has room.
            // Returns false when no such stack exists or every stack is already full.
            var itemStack = _items.FirstOrDefault(x => x.Value.Item.Id == itemId && x.Value.Quantity < x.Value.Item.MaxStackSize);

            if (itemStack.Key == null) return false;

            var item = itemStack.Value.Item;
            int quantity = itemStack.Value.Quantity;
            int newQuantity = Math.Min(quantity + amount, item.MaxStackSize);
            _items[item.InstanceId] = (item, newQuantity);
            ItemAmountChanges?.Invoke(item.InstanceId, newQuantity);
            return true;
        }

        public bool TryAddItem(IItem item, int amount = 1)
        {
            if (amount <= 0) return false;

            if (_items.Count >= InventoryCapacity)
            {
                InventoryFull?.Invoke(item.Id, item.InstanceId, amount, item.MaxStackSize);
                return false;
            }

            if (!_items.TryAdd(item.InstanceId, (Item: item, Quantity: amount))) return false;

            ItemAmountChanges?.Invoke(item.InstanceId, amount);
            return true;
        }

        public int GetAvailableCapacity() => Mathf.Max(InventoryCapacity - _items.Count, 0);

        public void RemoveItemById(string itemId, int amount = 1)
        {
            if (GetTotalItemAmount(itemId) < amount)
            {
                NotEnoughItems?.Invoke(itemId);
                return;
            }

            // Drain the lowest-quantity stacks first to keep larger stacks intact.
            // ToList() is required because we modify _itemInstances inside the loop.
            var stacks = _items
                .Where(x => x.Value.Item.Id == itemId)
                .OrderBy(x => x.Value.Quantity)
                .ToList();

            int remaining = amount;
            foreach ((string instanceId, (IItem item, int quantity)) in stacks)
            {
                if (remaining <= 0) break;

                int toRemove = Math.Min(quantity, remaining);
                remaining -= toRemove;
                int newQuantity = quantity - toRemove;

                if (newQuantity <= 0)
                    _items.Remove(instanceId);
                else
                    _items[instanceId] = (item, newQuantity);

                ItemAmountChanges?.Invoke(instanceId, newQuantity);
            }
        }

        public void RemoveItemByInstanceId(string instanceId)
        {
            if (!_items.Remove(instanceId)) return;
            ItemAmountChanges?.Invoke(instanceId, 0);
        }
    }
}
