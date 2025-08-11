namespace LastBreath.Script.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Godot;

    public class Inventory : IInventory
    {
        public event Action<IItem, MouseButtonPressed, IInventory>? ItemSlotClicked;
        public event Action? InventoryFull, NotEnougthItems;

        protected List<InventorySlot> Slots { get; } = [];

        public void Initialize(int size, GridContainer container)
        {
            for (int i = 0; i < size; i++)
            {
                InventorySlot inventorySlot = InventorySlot.Initialize().Instantiate<InventorySlot>();
                container.AddChild(inventorySlot);
                inventorySlot.OnItemClicked += OnItemSlotClicked;
                Slots.Add(inventorySlot);
            }
        }

        public void AddItem(IItem item)
        {
            var slot = GetSlotToAdd(item);
            if (slot == null)
            {
                // TODO: Log
                InventoryFull?.Invoke();
                return;
            }


            if (slot.CurrentItem != null)
            {
                int rest = slot.AddItemStacks(item.Quantity);
                if (rest > 0)
                {
                    var duplicate = item.Copy(true);
                    duplicate.Quantity = rest;
                    AddItem(duplicate);
                }
            }
            else
            {
                slot.AddNewItem(item);
            }
        }

        public void RemoveItem(string itemId, int amount = 1)
        {
            var slot = GetSlotToRemove(itemId);

            if (slot == null || slot.CurrentItem == null)
            {
                // TODO: Log
                return;
            }

            if (!slot.RemoveItemStacks(amount))
            {
                NotEnougthItems?.Invoke();
            }
        }

        public int GetNumberOfItems(IItem item) => Slots.FindAll(slot => slot.CurrentItem != null && slot.CurrentItem.Id == item.Id).Count;

        public List<IItem?> GiveAllItems() => [.. Slots.Where(x => x.CurrentItem != null).Select(x => x.CurrentItem)];

        public void TakeAllItems(List<IItem?> items)
        {
            if (items.Count > 0)
                items.ForEach(AddItem!);
        }

        public void Clear() => Slots.ForEach(slot => slot.ClearSlot());
        private InventorySlot? GetSlotToAdd(IItem item) => Slots.FirstOrDefault(itemSlot => itemSlot.CurrentItem == null || itemSlot.CurrentItem.Id == item.Id && itemSlot.Quantity < item.MaxStackSize);
        private InventorySlot? GetSlotToRemove(string itemId) => Slots.FirstOrDefault(itemSlot => itemSlot.CurrentItem != null && itemSlot.CurrentItem.Id == itemId);
        private void OnItemSlotClicked(IItem item, MouseButtonPressed pressed) => ItemSlotClicked?.Invoke(item, pressed, this);
    }
}
