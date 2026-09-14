namespace Core.World.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Inventory;
    using Items;

    public sealed class ChestSlot(string id, IItem? item, int amount)
    {
        public string Id { get; } = id;
        public IItem? Item { get; } = item;
        public int Amount { get; internal set; } = amount;
    }

    public record ChestTransfer(int Accepted, bool CapacityLimited);

    /// <summary>Owns concrete contents and commits source quantities before inventory notifications escape.</summary>
    public sealed class ChestContents
    {
        private bool _transferring;
        public bool Initialized { get; private set; }
        public List<ChestSlot> Slots { get; private set; } = [];
        public double? RemoveAtMinutes { get; private set; }
        public bool Empty => Initialized && Slots.All(x => x.Amount == 0);

        public void Initialize(ChestDefinition definition, Func<AuthoredChestItem, IItem> create)
        {
            if (Initialized) return;
            var slots = definition.Items.Select(x => new ChestSlot(x.SlotId, create(x), x.Amount)).ToList();
            Restore(true, slots, null);
        }

        public void Restore(bool initialized, List<ChestSlot> slots, double? removeAt)
        {
            if (slots.Select(x => x.Id).Distinct().Count() != slots.Count
                || slots.Any(x => string.IsNullOrWhiteSpace(x.Id) || x.Amount < 0 || x.Amount > 0 && x.Item == null)
                || !initialized && (slots.Count > 0 || removeAt != null)
                || removeAt is { } deadline && (!double.IsFinite(deadline) || deadline < 0 || slots.Any(x => x.Amount > 0))
                || initialized && slots.All(x => x.Amount == 0) && removeAt == null)
                throw new InvalidOperationException("Invalid chest state.");
            Initialized = initialized;
            Slots = slots;
            RemoveAtMinutes = removeAt;
        }

        public ChestTransfer Transfer(IInventoryTransfer inventory, string? slotId, double now, double delay, Func<bool> stillAccessible)
        {
            if (_transferring || !Initialized) return new(0, false);
            _transferring = true;
            try
            {
                using var notifications = inventory.DeferNotifications();
                int accepted = 0;
                bool limited = false;
                foreach (var slot in Slots.Where(x => slotId == null || x.Id == slotId))
                {
                    if (!stillAccessible()) break;
                    if (slot.Amount == 0 || slot.Item == null) continue;
                    int moved = inventory.ReceiveUpTo(slot.Item, slot.Amount);
                    if (moved < 0 || moved > slot.Amount) throw new InvalidOperationException("Invalid inventory acceptance.");
                    slot.Amount -= moved;
                    accepted += moved;
                    limited |= slot.Amount > 0;
                }
                if (Empty) RemoveAtMinutes ??= now + delay;
                return new(accepted, limited);
            }
            finally { _transferring = false; }
        }

        public bool RemovalDue(double now) => Empty && RemoveAtMinutes is { } deadline && now >= deadline;
    }
}
