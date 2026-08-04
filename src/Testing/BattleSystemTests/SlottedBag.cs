namespace LastBreathTest.BattleSystemTests
{
    using Core.Inventory;
    using Core.Items;

    /// <summary>
    /// The bag as the game's own behaves, without the scene it is built out of. The real one is a
    /// grid of engine nodes and cannot stand up outside the runtime, so what it does with an item is
    /// mirrored here and nothing beyond it: an arriving stack first tops up occupied slots holding
    /// the SAME item id while those have room, whatever is left takes empty slots, and the bag keeps
    /// a second register of the instances themselves — the slot remembers an id and a count, the
    /// register remembers the thing.
    /// <para>
    /// Mirrored faithfully includes the part that is wrong: a stack that found no empty slot is
    /// dropped and the caller is still told it was taken.
    /// </para>
    /// </summary>
    internal sealed class SlottedBag(int slots) : IInventory
    {
        private readonly Dictionary<string, IItem> _instances = [];
        private readonly Slot?[] _slots = new Slot?[slots];

        public int InventoryCapacity => _slots.Length;

        public event Action<string, int>? ItemAmountChanges;

        /// <summary>What stands in the slots, in the order they were filled — the shape of the bag
        /// rather than its contents, which is where merging shows.</summary>
        public IReadOnlyList<(IItem Item, int Quantity)> OccupiedSlots =>
            [.. _slots.OfType<Slot>().Select(slot => (slot.Item, slot.Quantity))];

        public List<string> GetAllItemIdsWithTag(string tag) =>
            [.. _instances.Values.Where(item => item.HasTag(tag)).Select(item => item.Id)];

        public T? GetItem<T>(string instanceId)
            where T : class, IItem => _instances.GetValueOrDefault(instanceId) as T;

        public int GetTotalItemAmount(string itemId) =>
            _slots.OfType<Slot>().Where(slot => slot.Item.Id == itemId).Sum(slot => slot.Quantity);

        public bool TryAddItem(IItem item, int amount = 1)
        {
            if (amount <= 0) return false;

            _instances.TryAdd(item.InstanceId, item);
            Fit(item, amount);
            ItemAmountChanges?.Invoke(item.Id, GetTotalItemAmount(item.Id));
            return true;
        }

        public bool TryAddItemStacks(string itemId, int amount = 1)
        {
            var held = _instances.Values.FirstOrDefault(item => item.Id == itemId);
            if (held == null) return false;

            Fit(held, amount);
            return true;
        }

        public int GetAvailableCapacity() => _slots.Count(slot => slot == null);

        public IReadOnlyList<(IItem Item, int Amount)> GetContents() =>
        [
            .. _slots.OfType<Slot>()
                .GroupBy(slot => slot.Item.InstanceId)
                .Select(group => (group.First().Item, group.Sum(slot => slot.Quantity)))
        ];

        public void RemoveItemById(string itemId, int amount = 1)
        {
            int remaining = amount;
            foreach (Slot slot in _slots.OfType<Slot>().Where(slot => slot.Item.Id == itemId).OrderBy(slot => slot.Quantity))
            {
                if (remaining <= 0) break;
                int removed = Math.Min(slot.Quantity, remaining);
                slot.Quantity -= removed;
                remaining -= removed;
                if (slot.Quantity <= 0) Empty(slot);
            }
        }

        public void RemoveItemByInstanceId(string instanceId)
        {
            foreach (Slot slot in _slots.OfType<Slot>().Where(slot => slot.Item.InstanceId == instanceId))
                Empty(slot);
        }

        public void Clear()
        {
            Array.Clear(_slots);
            _instances.Clear();
        }

        private void Fit(IItem item, int amount)
        {
            int remaining = amount;
            foreach (Slot slot in _slots.OfType<Slot>().Where(slot => slot.Item.Id == item.Id))
            {
                if (remaining <= 0) break;
                int added = Math.Min(slot.Item.MaxStackSize - slot.Quantity, remaining);
                slot.Quantity += added;
                remaining -= added;
            }

            for (int index = 0; index < _slots.Length && remaining > 0; index++)
            {
                if (_slots[index] != null) continue;
                int placed = Math.Min(item.MaxStackSize, remaining);
                _slots[index] = new Slot(item, placed);
                remaining -= placed;
            }
        }

        private void Empty(Slot emptied)
        {
            for (int index = 0; index < _slots.Length; index++)
                if (ReferenceEquals(_slots[index], emptied))
                    _slots[index] = null;

            if (_slots.OfType<Slot>().All(slot => slot.Item.InstanceId != emptied.Item.InstanceId))
                _instances.Remove(emptied.Item.InstanceId);
        }

        private sealed class Slot(IItem item, int quantity)
        {
            public IItem Item { get; } = item;

            public int Quantity { get; set; } = quantity;
        }
    }
}
