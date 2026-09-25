namespace Crafting.Internal.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
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

        /// <summary>
        /// Tops up an existing stack of the item, all of the amount or none of it: a stack with room
        /// for only part of it is left alone, so the caller opens a fresh one instead of the remainder
        /// being trimmed away in silence. <see langword="false"/> when no held stack can take it whole.
        /// </summary>
        public bool TryAddItemStacks(string itemId, int amount = 1)
        {
            var itemStack = _items.FirstOrDefault(x =>
                x.Value.Item.Id == itemId && x.Value.Quantity + amount <= x.Value.Item.MaxStackSize);

            if (itemStack.Key == null) return false;

            var item = itemStack.Value.Item;
            int newQuantity = itemStack.Value.Quantity + amount;
            _items[item.InstanceId] = (item, newQuantity);
            ItemAmountChanges?.Invoke(item.InstanceId, newQuantity);
            return true;
        }

        /// <summary>
        /// Takes the item, all of the amount or none of it. The sandbox keeps one entry per instance
        /// instead of a grid of slots, so it refuses three ways: a non-positive amount, a bag at
        /// capacity (announced through <see cref="InventoryFull"/>), and an instance already held —
        /// the same instance cannot lie in the bag twice, and that refusal comes with room to spare.
        /// Every one of them leaves the bag exactly as it was, the caller still holding the item.
        /// </summary>
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

        public IReadOnlyList<(IItem Item, int Amount)> GetContents() =>
            _items.Values.Select(entry => (entry.Item, entry.Quantity)).ToList();

        public void Clear()
        {
            var ids = _items.Keys.ToList();
            _items.Clear();
            foreach (var id in ids) ItemAmountChanges?.Invoke(id, 0);
        }
    }
}
