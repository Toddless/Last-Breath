namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Godot;

    /// <summary>
    /// A bag of ids and counts. The narrative asks a bag three things — how many of an id it holds,
    /// take some away, put some in — and a dry run answers those and nothing else: an item here has no
    /// rolls, no icon and no price, because no entry of the vocabulary reads any of them.
    /// </summary>
    public sealed class SandboxBag : IInventory
    {
        /// <summary>How many distinct stacks the bag takes. A quest turn-in is gated on free room, so
        /// the number has to be real; it is authored nowhere, and a bag this size never refuses a
        /// reward for a reason its reader did not put there.</summary>
        public const int Capacity = 60;

        private readonly Dictionary<string, int> _amounts = new(StringComparer.Ordinal);

        public int InventoryCapacity => Capacity;

        /// <summary>What the bag holds now, for the panel that shows a run's world beside the one its
        /// author typed.</summary>
        public IReadOnlyDictionary<string, int> Amounts => _amounts;

        public event Action<string, int>? ItemAmountChanges;

        /// <summary>Puts the authored bag back, without waking the quests: a reset is not a pick-up.</summary>
        public void Restore(IReadOnlyDictionary<string, int> amounts)
        {
            _amounts.Clear();
            foreach ((string id, int amount) in amounts)
                if (amount > 0) _amounts[id] = amount;
        }

        public List<string> GetAllItemIdsWithTag(string tag) => [];

        public T? GetItem<T>(string instanceId) where T : class, IItem => null;

        public int GetTotalItemAmount(string id) => _amounts.GetValueOrDefault(id);

        public bool TryAddItem(IItem item, int amount = 1) => TryAddItemStacks(item.Id, amount);

        public int GetAvailableCapacity() => Math.Max(0, Capacity - _amounts.Count);

        public void RemoveItemById(string itemId, int amount = 1)
        {
            if (amount <= 0 || !_amounts.TryGetValue(itemId, out int held)) return;

            int left = held - amount;
            if (left > 0) _amounts[itemId] = left;
            else _amounts.Remove(itemId);

            ItemAmountChanges?.Invoke(itemId, Math.Max(0, left));
        }

        /// <summary>Nothing in the narrative addresses a single instance — a dry run holds counts.</summary>
        public void RemoveItemByInstanceId(string instanceId)
        {
        }

        public bool TryAddItemStacks(string itemId, int amount = 1)
        {
            if (amount <= 0) return false;
            if (!_amounts.ContainsKey(itemId) && GetAvailableCapacity() == 0) return false;

            _amounts[itemId] = GetTotalItemAmount(itemId) + amount;
            ItemAmountChanges?.Invoke(itemId, _amounts[itemId]);
            return true;
        }

        public IReadOnlyList<(IItem Item, int Amount)> GetContents() =>
            [.. _amounts.Select(entry => ((IItem)new SandboxItem(entry.Key), entry.Value))];

        public void Clear() => _amounts.Clear();
    }

    /// <summary>An item as a dry run needs it: the id it was named by, and the answers the interfaces
    /// demand. Nothing reads the rest, and rolling a real one would need the whole item pipeline.</summary>
    public sealed class SandboxItem(string id) : IItem
    {
        private const int StackSize = 999;

        public string Id { get; } = id;

        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public string[] Tags { get; } = [];

        public Rarity Rarity { get; set; }

        public int MaxStackSize => StackSize;

        public Texture2D? Icon => null;

        public string Description => Id;

        public string DisplayName => Id;

        public bool IsSame(string otherId) => string.Equals(InstanceId, otherId, StringComparison.Ordinal);

        public bool HasTag(string tag) => false;

        public T Copy<T>() => (T)(object)new SandboxItem(Id);
    }

    /// <summary>Every id names a plain stack here: a reward is written down by the id it was granted
    /// under, which is the whole of what a run has to show about it.</summary>
    public sealed class SandboxItemMinter : IItemMinter
    {
        public IItem MintItem(string id, Rarity? rarity = null) => new SandboxItem(id) { Rarity = rarity ?? Rarity.Common };
    }

    /// <summary>No catalog of one-of-a-kind rewards is read, so nothing is written down as handed over
    /// once: a repeated turn-in in a dry run pays the same list again.</summary>
    public sealed class SandboxUniqueItems : IUniqueItemQuery
    {
        public bool IsUnique(string itemId) => false;
    }
}
