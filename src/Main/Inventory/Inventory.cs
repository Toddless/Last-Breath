namespace LastBreath.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Session;
    using Core.Views.UI;
    using Godot;
    using UI;

    public class Inventory(IUiElementsManager uiElements, IGameMessageBus messageBus) : IInventory, ISlotLender, ISessionResettable
    {
        private const int BagSlots = 220;
        private const string BagIsFullKey = "UI_Inventory_Full";
        private const string MissingItemsKey = "UI_Inventory_Missing_Items";

        private readonly Dictionary<string, IItem> _itemInstances = [];
        protected List<IInventorySlot> Slots { get; } = [];

        public int InventoryCapacity => BagSlots;

        public event Action<string, int>? ItemAmountChanges;
        public event Action<IItem, MouseInteractions>? ItemInteraction;

        /// <summary>An equipped piece was dragged into a bag slot; the window resolves the unequip.</summary>
        public event Action<EquipmentPiece, IInventorySlot>? EquipmentDroppedIntoBag;

        /// <summary>
        /// Shows the bag inside a window's grid. The SERVICE owns the slot nodes for the whole
        /// session (the items live in them); windows only borrow the view — detach before dying.
        /// </summary>
        public void AttachSlots(GridContainer container)
        {
            EnsureSlots();
            foreach (var slot in Slots)
            {
                if (slot is not Slot node) continue;
                if (node.GetParent() == null) container.AddChild(node);
                // A held instance may have mutated in place since the bag was last shown
                // (ascension flips the rarity) — the borrowed views redraw from the live item.
                node.RefreshView();
            }
        }

        /// <summary>Fresh-instance windows die on close; the slots must not die with them.</summary>
        public void DetachSlots()
        {
            foreach (var slot in Slots)
                if (slot is Node node)
                    node.GetParent()?.RemoveChild(node);
        }

        private void EnsureSlots()
        {
            if (Slots.Count > 0) return;

            for (int i = 0; i < BagSlots; i++)
            {
                InventorySlot inventorySlot = InventorySlot.Initialize().Instantiate<InventorySlot>();
                inventorySlot.ItemDeleted += OnDeleteRequested;
                inventorySlot.GetItemInstance = (GetItem<IItem>);
                inventorySlot.GetItemIcon = GetItemIcon;
                inventorySlot.ItemInteraction += OnItemInteraction;
                inventorySlot.EquipmentDropped += (piece, slot) => EquipmentDroppedIntoBag?.Invoke(piece, slot);
                HoverTooltip.Attach(inventorySlot, () => ShowItemTooltip(inventorySlot));
                Slots.Add(inventorySlot);
            }
        }

        private IPopup? ShowItemTooltip(IInventorySlot slot)
        {
            if (slot.CurrentItem == null) return null;
            var item = GetItem<IItem>(slot.CurrentItem.InstanceId);
            if (item == null) return null;

            var popup = uiElements.ShowPopup(typeof(ItemTooltipPopup)) as ItemTooltipPopup;
            popup?.ShowItem(item);
            return popup;
        }

        private void OnItemInteraction(IInventorySlot slot, MouseInteractions interactions)
        {
            if (slot.CurrentItem == null) return;
            var item = GetItem<IItem>(slot.CurrentItem.InstanceId);
            if (item != null) ItemInteraction?.Invoke(item, interactions);
        }

        public List<string> GetAllItemIdsWithTag(string tag) => [.. _itemInstances.Values.Where(x => x.HasTag(tag)).Select(x => x.Id)];
        T? IInventory.GetItem<T>(string instanceId) where T : class => _itemInstances.GetValueOrDefault(instanceId) as T;

        public T? GetItem<T>(string instanceId)
            where T : IItem => (T?)_itemInstances.GetValueOrDefault(instanceId);
        public int GetTotalItemAmount(string itemId) => Slots.Where(x => x.CurrentItem != null && x.CurrentItem.ItemId == itemId).Sum(x => x.Quantity);
        /// <summary>
        /// Adds the stack of the item to the existing instance. <see langword="true"/> only when the
        /// whole amount was added; <see langword="false"/> when no instance of the id is held or the
        /// bag has no room for all of it, and a refusal leaves the bag exactly as it was.
        /// </summary>
        /// <param name="itemId">Meaningful Id. For example "Crafting_Resource_Diamond"</param>
        /// <param name="amount">How many units of the item are being added.</param>
        public bool TryAddItemStacks(string itemId, int amount = 1)
        {
            var itemInstance = _itemInstances.Values.FirstOrDefault(x => x.Id == itemId);
            if (itemInstance == null) return false;

            if (!HasRoomFor(itemId, amount, itemInstance.MaxStackSize))
            {
                NotifyToast(BagIsFullKey, itemInstance.DisplayName);
                return false;
            }

            FitItemsInSlots(itemInstance.Id, itemInstance.InstanceId, amount, itemInstance.MaxStackSize);
            return true;
        }

        /// <summary>
        /// Takes the item into the bag, all of it or none of it. An amount that does not fit whole is
        /// not placed at all and leaves neither the slots nor the instance register touched, so a
        /// caller answered <see langword="false"/> still owns what it tried to hand over.
        /// </summary>
        public bool TryAddItem(IItem item, int amount = 1)
        {
            if (amount <= 0) return false;

            if (!HasRoomFor(item.Id, amount, item.MaxStackSize))
            {
                NotifyToast(BagIsFullKey, item.DisplayName);
                return false;
            }

            // The register is filled before the slots: a slot draws the item it was given through it.
            _itemInstances.TryAdd(item.InstanceId, item);

            FitItemsInSlots(item.Id, item.InstanceId, amount, item.MaxStackSize);

            AnnounceAmountChange(item.Id);

            return true;
        }

        /// <summary>Every announced change also redraws ALL borrowed slot views: a craft operation
        /// spends resources through here while it mutates an equip instance in place (ascension),
        /// and the mutated item's own slot never sees an event of its own.</summary>
        private void AnnounceAmountChange(string itemId)
        {
            ItemAmountChanges?.Invoke(itemId, GetTotalItemAmount(itemId));
            foreach (var slot in Slots)
                if (slot is Slot node)
                    node.RefreshView();
        }

        /// <summary>Whether the whole amount fits: what the stacks already held can still take, plus
        /// the empty slots.</summary>
        private bool HasRoomFor(string itemId, int amount, int maxStackSize)
        {
            EnsureSlots(); // items arrive (loot, quests) long before any window shows the bag
            int room = 0;
            foreach (var slot in Slots)
            {
                room += RoomInSlot(slot, itemId, maxStackSize);
                if (room >= amount) return true;
            }

            return room >= amount;
        }

        /// <summary>How much of the item a single slot can take: an empty slot a whole stack, a slot
        /// already holding the same item whatever is left of its own, any other slot nothing.</summary>
        private static int RoomInSlot(IInventorySlot slot, string itemId, int maxStackSize)
        {
            if (slot.CurrentItem == null) return maxStackSize;
            return slot.CurrentItem.ItemId == itemId ? slot.CurrentItem.MaxStackSize - slot.Quantity : 0;
        }

        public int GetAvailableCapacity()
        {
            EnsureSlots();
            return Slots.Count(x => x.CurrentItem == null);
        }

        /// <summary>One entry per held instance (equip items are unique; a resource's slots sum up).</summary>
        public IReadOnlyList<(IItem Item, int Amount)> GetContents()
        {
            EnsureSlots();
            return Slots
                .Where(slot => slot.CurrentItem != null)
                .GroupBy(slot => slot.CurrentItem!.InstanceId)
                .Select(group => (Item: _itemInstances.GetValueOrDefault(group.Key), Amount: group.Sum(slot => slot.Quantity)))
                .Where(entry => entry.Item != null)
                .Select(entry => (entry.Item!, entry.Amount))
                .ToList();
        }

        /// <summary>Repacks the bag: stacks merged, best rarity first, then by name.</summary>
        public void SortBag()
        {
            EnsureSlots();
            // Instances resolve BEFORE the clear: a stack whose instance went missing is skipped
            // instead of losing the whole re-fill to an exception over an empty bag.
            var stacks = Slots
                .Where(slot => slot.CurrentItem != null)
                .Select(slot => (Item: _itemInstances.GetValueOrDefault(slot.CurrentItem!.InstanceId), slot.Quantity))
                .Where(x => x.Item != null)
                .ToList();

            foreach (var slot in Slots)
                slot.ClearSlot();

            // Stackables merge by item id (each pickup is its own instance); per-roll items keep instance identity.
            var ordered = stacks
                .GroupBy(x => x.Item!.MaxStackSize > 1 ? x.Item.Id : x.Item.InstanceId)
                .Select(group => (Item: group.First().Item!, Amount: group.Sum(x => x.Quantity)))
                .OrderBy(x => x.Item.Rarity.DisplayRank())
                .ThenBy(x => x.Item.DisplayName, StringComparer.Ordinal);

            foreach (var (item, amount) in ordered)
                FitItemsInSlots(item.Id, item.InstanceId, amount, item.MaxStackSize);
        }

        /// <summary>Adds an item into the specific slot the player dropped it on; an occupied slot falls back to the usual placement.</summary>
        public bool TryAddItemAt(IItem item, IInventorySlot slot)
        {
            if (slot.CurrentItem != null) return TryAddItem(item);

            _itemInstances[item.InstanceId] = item;
            slot.SetItem(new(item.Id, item.InstanceId, item.MaxStackSize));
            AnnounceAmountChange(item.Id);
            return true;
        }

        public void RemoveItemById(string itemId, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(itemId)) return;

            int remainToDelete = amount;

            // Fixed before the first slot is touched: the announcement at the end reaches listeners that
            // add and take items of their own, and the set being emptied must not shift under them.
            var slotsWithItem = Slots.Where(x => x.CurrentItem?.ItemId == itemId).OrderBy(x => x.Quantity).ToList();

            foreach (var slot in slotsWithItem)
            {
                if (remainToDelete <= 0) break;
                int canRemove = Mathf.Min(remainToDelete, slot.Quantity);
                if (slot.TryRemoveItemStacks(canRemove))
                    remainToDelete -= canRemove;
                else
                    Tracker.TrackError($"Failed to remove {canRemove} x '{itemId}' from an inventory slot");
            }
            if (remainToDelete > 0) NotifyToast(MissingItemsKey, DisplayNameOf(itemId));

            AnnounceAmountChange(itemId);
        }

        public void RemoveItemByInstanceId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) return;

            // Read before the slots go: the count is announced under the ITEM's id, and the instance is
            // forgotten on the way. Fixed before the first slot is cleared for the same reason the other
            // removal fixes its set — listeners add and take items of their own.
            string? itemId = _itemInstances.GetValueOrDefault(instanceId)?.Id;

            foreach (var slot in Slots.Where(x => x.CurrentItem?.InstanceId == instanceId).ToList())
                slot.ClearSlot(isDeleted: true);

            if (itemId != null) AnnounceAmountChange(itemId);
        }

        public void Clear()
        {
            Slots.ForEach(slot => slot.ClearSlot());
            _itemInstances.Clear();
        }

        /// <summary>The slot nodes are service-owned for the whole process — a new session empties them, not replaces them.</summary>
        public void ResetSession() => Clear();

        /// <summary>A slot gave its item up. The register holds the instance while any slot still does —
        /// a stack lies across several — and drops it with the last. The count is announced by the
        /// removal that emptied the slot, once it has finished walking them.</summary>
        protected void OnDeleteRequested(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) return;
            if (!_itemInstances.TryGetValue(instanceId, out var toRemove))
            {
                Tracker.TrackNotFound($"Item with id: {instanceId}", this);
                return;
            }

            if (Slots.Any(x => x.CurrentItem?.InstanceId == instanceId))
                return;

            _itemInstances.Remove(toRemove.InstanceId);
        }

        private Texture2D? GetItemIcon(string instanceId) => _itemInstances[instanceId].Icon;

        /// <summary>The name a held instance carries; an id nothing is held under speaks for itself.</summary>
        private string DisplayNameOf(string itemId) =>
            _itemInstances.Values.FirstOrDefault(x => x.Id == itemId)?.DisplayName ?? itemId;

        private void NotifyToast(string key, string itemName) =>
            _ = messageBus.PublishMessageAsync(new SendNotificationMessageMessage(key, NotificationCategory.System,
                new Dictionary<string, object?> { ["Item"] = itemName }));

        /// <summary>Lays the amount out over the slots. Runs behind a room check — the placement
        /// itself never decides to drop anything.</summary>
        private void FitItemsInSlots(string itemId, string instanceId, int amount, int maxStackSize)
        {
            EnsureSlots(); // items arrive (loot, quests) long before any window shows the bag
            int remaining = amount;
            // Top-up matches by item id, not instance: every pickup of a resource is a fresh
            // instance, matching by instance would fragment the bag into per-pickup stacks.
            // Per-roll items (equip) are safe — their stack size of 1 never merges.
            var slotsWithSameItem = Slots.Where(x => x.CurrentItem?.ItemId == itemId);
            foreach (var slot in slotsWithSameItem)
            {
                if (remaining <= 0) break;
                slot.TryAddStacks(remaining, out int left);
                remaining = left;
            }

            // add what left in empty slots
            while (remaining > 0)
            {
                var emptySlot = Slots.FirstOrDefault(x => x.CurrentItem == null);
                if (emptySlot == null)
                {
                    // Unreachable while every entry point checks the room first: the bag lost count of itself.
                    Tracker.TrackError($"No slot left for {remaining} x '{itemId}' the bag reported room for");
                    return;
                }

                int toAdd = Mathf.Min(maxStackSize, remaining);
                emptySlot.SetItem(new(itemId, instanceId, maxStackSize), toAdd);
                remaining -= toAdd;
            }
        }

    }
}
