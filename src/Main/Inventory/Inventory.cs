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

    public class Inventory(IUiElementsManager uiElements, IGameMessageBus messageBus) : IInventory, ISessionResettable
    {
        private const int BagSlots = 220;

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
                if (slot is Node node && node.GetParent() == null)
                    container.AddChild(node);
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

            _itemInstances.TryAdd(item.InstanceId, item);

            FitItemsInSlots(item.Id, item.InstanceId, amount, item.MaxStackSize);

            ItemAmountChanges?.Invoke(item.Id, GetTotalItemAmount(item.Id));

            return true;
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
                .OrderBy(x => RarityRank(x.Item.Rarity))
                .ThenBy(x => x.Item.DisplayName, StringComparer.Ordinal);

            foreach (var (item, amount) in ordered)
                FitItemsInSlots(item.Id, item.InstanceId, amount, item.MaxStackSize);
        }

        /// <summary>Display order: best first — the enum itself puts Unique/Mythic at 10/11.</summary>
        private static int RarityRank(Rarity rarity) => rarity switch
        {
            Rarity.Mythic => 0,
            Rarity.Unique => 1,
            _ => (int)rarity + 2,
        };

        /// <summary>Adds an item into the specific slot the player dropped it on; an occupied slot falls back to the usual placement.</summary>
        public bool TryAddItemAt(IItem item, IInventorySlot slot)
        {
            if (slot.CurrentItem != null) return TryAddItem(item);

            _itemInstances[item.InstanceId] = item;
            slot.SetItem(new(item.Id, item.InstanceId, item.MaxStackSize));
            ItemAmountChanges?.Invoke(item.Id, GetTotalItemAmount(item.Id));
            return true;
        }

        public void RemoveItemById(string itemId, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(itemId)) return;

            int remainToDelete = amount;

            var slotsWithItem = Slots.Where(x => x.CurrentItem?.ItemId == itemId).OrderBy(x => x.Quantity);

            foreach (var slot in slotsWithItem)
            {
                if (remainToDelete <= 0) break;
                int canRemove = Mathf.Min(remainToDelete, slot.Quantity);
                if (slot.TryRemoveItemStacks(canRemove))
                    remainToDelete -= canRemove;
                else
                    Tracker.TrackError($"Failed to remove {canRemove} x '{itemId}' from an inventory slot");
            }
            if (remainToDelete > 0) NotifyToast("UI_Inventory_Missing_Items", itemId);

            ItemAmountChanges?.Invoke(itemId, GetTotalItemAmount(itemId));
        }

        public void RemoveItemByInstanceId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) return;

            var slots = Slots.Where(x => x.CurrentItem?.InstanceId == instanceId);
            foreach (var slot in slots)
                slot.ClearSlot(isDeleted: true);
        }

        public void Clear()
        {
            Slots.ForEach(slot => slot.ClearSlot());
            _itemInstances.Clear();
        }

        /// <summary>The slot nodes are service-owned for the whole process — a new session empties them, not replaces them.</summary>
        public void ResetSession() => Clear();

        protected void OnDeleteRequested(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            if (!_itemInstances.TryGetValue(itemId, out var toRemove))
            {
                Tracker.TrackNotFound($"Item with id: {itemId}", this);
                return;
            }

            if (Slots.Any(x => x.CurrentItem?.InstanceId == itemId))
                return;

            _itemInstances.Remove(toRemove.InstanceId);
            ItemAmountChanges?.Invoke(toRemove.Id, GetTotalItemAmount(itemId));
        }

        private Texture2D? GetItemIcon(string instanceId) => _itemInstances[instanceId].Icon;

        private void NotifyToast(string key, string itemId)
        {
            string name = _itemInstances.Values.FirstOrDefault(x => x.Id == itemId)?.DisplayName ?? itemId;
            _ = messageBus.PublishMessageAsync(new SendNotificationMessageMessage(key, NotificationCategory.System,
                new Dictionary<string, object?> { ["Item"] = name }));
        }

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
                    // The overflow is dropped; without the toast the loss would be silent.
                    NotifyToast("UI_Inventory_Full", itemId);
                    return;
                }

                int toAdd = Mathf.Min(maxStackSize, remaining);
                emptySlot.SetItem(new(itemId, instanceId, maxStackSize), toAdd);
                remaining -= toAdd;
            }
        }

    }
}
