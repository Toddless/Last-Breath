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
        public event Action<string, MouseButtonPressed, IInventory>? ItemSlotClicked;
        public event Action? InventoryFull, NotEnougthItems;
        public event Action<string, int>? ItemAmountChanges;

        protected List<IInventorySlot> Slots { get; } = [];

        public void Initialize(int size, GridContainer container)
        {
            for (int i = 0; i < size; i++)
            {
                InventorySlot inventorySlot = InventorySlot.Initialize().Instantiate<InventorySlot>();
                container.AddChild(inventorySlot);
                //inventorySlot.OnItemClicked += OnItemSlotClicked;
                Slots.Add(inventorySlot);
            }
        }


        public IInventorySlot? GetSlotWithItemOrNull(string id) => Slots.FirstOrDefault(x => x.CurrentItem == id);

        public List<IInventorySlot> GetAllSlotsWithItemsWithTag(string tag) => Slots.FindAll(x => x.CurrentItem != null && x.ItemHasTag(tag));

        public void AddItem(IItem item, int amount = 1)
        {
            var slot = GetSlotToAdd(item);
            if (slot == null)
            {
                Logger.LogInfo("Cannot find slot without item", this);
                InventoryFull?.Invoke();
                return;
            }


            if (slot.CurrentItem != null)
            {
                int rest = slot.AddItemStacks(amount);
                if (rest > 0)
                    AddItem(item, rest);
            }
            else
            {
                slot.SetItem(item.Id, amount);
            }
        }

        public void RemoveItem(string itemId, int amount = 1)
        {
            var slot = GetSlotToRemove(itemId);

            if (slot == null)
            {
                Logger.LogNull(itemId, this);
                return;
            }
            if (slot.CurrentItem == null)
            {
                Logger.LogNull($"Slot with item: {itemId} was found, but item", this);
                return;
            }

            if (!slot.RemoveItemStacks(amount))
            {
                NotEnougthItems?.Invoke();
            }
            else
            {
                ItemAmountChanges?.Invoke(itemId, slot.Quantity);
            }
        }


        public void Clear() => Slots.ForEach(slot => slot.ClearSlot());
        private IInventorySlot? GetSlotToAdd(IItem item) => Slots.FirstOrDefault(itemSlot => itemSlot.CurrentItem == null || itemSlot.CurrentItem == item.Id && itemSlot.Quantity < item.MaxStackSize);
        private IInventorySlot? GetSlotToRemove(string itemId) => Slots.FirstOrDefault(itemSlot => itemSlot.CurrentItem != null && itemSlot.CurrentItem == itemId);
       // private void OnItemSlotClicked(IItem item, MouseButtonPressed pressed) => ItemSlotClicked?.Invoke(item.Id, pressed, this);
    }
}
