namespace Playground.Script.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Items;

    public class Inventory
    {
        public event Action<Item, MouseButtonPressed, Inventory>? SlotClicked;
        public event Action? InventoryFull, NotEnougthItems;

        protected List<InventorySlot> Slots { get; } = [];

        public void Initialize(int size, GridContainer container)
        {
            for (int i = 0; i < size; i++)
            {
                InventorySlot inventorySlot = InventorySlot.Initialize().Instantiate<InventorySlot>();
                container.AddChild(inventorySlot);
                inventorySlot.OnClick += OnSlotClicked;
                Slots.Add(inventorySlot);
            }
        }

        public void AddItem(Item item)
        {
            var slot = GetSlotToAdd(item);
            if (slot == null)
            {
                // TODO: Log
                InventoryFull?.Invoke();
                return;
            }


            if (slot.Item != null)
            {
                int rest = slot.AddItemStacks(item.Quantity);
                Item duplicate = (Item)item.Duplicate(true);
                duplicate.Quantity = rest;
                AddItem(duplicate);
            }
            else
            {
                slot.AddNewItem(item);
            }
        }

        public void RemoveItem(string itemId, int amount = 1)
        {
            var slot = GetSlotToRemove(itemId);

            if (slot == null || slot.Item == null)
            {
                // TODO: Log
                return;
            }

            if (!slot.RemoveItemStacks(amount))
            {
                NotEnougthItems?.Invoke();
            }
        }

        public int GetNumberOfItems(Item item) => Slots.FindAll(slot => slot.Item != null && slot.Item.Id == item.Id).Count;

        public List<Item?> GiveAllItems() => [.. Slots.Where(x => x.Item != null).Select(x => x.Item)];

        public void TakeAllItems(List<Item?> items)
        {
            if (items.Count > 0)
                items.ForEach(AddItem!);
        }

        public void Clear() => Slots.ForEach(slot => slot.ClearSlot());
        private InventorySlot? GetSlotToAdd(Item item) => Slots.FirstOrDefault(itemSlot => itemSlot.Item == null || (itemSlot.Item.Id == item.Id && itemSlot.Quantity < item.MaxStackSize));
        private InventorySlot? GetSlotToRemove(string itemId) => Slots.FirstOrDefault(itemSlot => itemSlot.Item != null && itemSlot.Item.Id == itemId);
        private void OnSlotClicked(Item item, MouseButtonPressed pressed) => SlotClicked?.Invoke(item, pressed, this);
    }
}
