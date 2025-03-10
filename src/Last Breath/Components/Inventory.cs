namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public class Inventory
    {
        private readonly List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;

        public event Action<string>? SpecialItemAdded;

        public void Initialize(int size, GridContainer container)
        {
            _inventorySlot = InventorySlot.Initialize();
            for (int i = 0; i < size; i++)
            {
                InventorySlot inventorySlot = _inventorySlot!.Instantiate<InventorySlot>();
                container.AddChild(inventorySlot);
                _slots.Add(inventorySlot);
            }
        }

        public void AddItem(Item item)
        {
            var slot = GetSlotToAdd(item);
            if (slot == null)
            {
                return;
            }

            if(slot.Item != null)
            {
                slot.AddItem(item);
            }
            else
            {
                slot.SetItem(item);
            }
        }

        public InventorySlot? GetSlotToAdd(Item item)
        {
            return _slots.FirstOrDefault(itemSlot => itemSlot.Item == null || (itemSlot.Item.Guid == item.Guid && itemSlot.Item.MaxStackSize > item.Quantity));
        }

        public InventorySlot? GetSlotToRemove(Item? item)
        {
            return _slots.FirstOrDefault(x => x.Item != null && x.Item.Guid == item?.Guid);
        }

        public void RemoveItem(Item item)
        {
            var slot = GetSlotToRemove(item);

            if (slot == null)
            {
                return;
            }
            slot.RemoveItem(item);
        }

        public int GetNumberOfItems(Item item)
        {
            return _slots.FindAll(slot => slot.Item != null && slot.Item.Guid == item.Guid).Count;
        }

        public List<Item?> GiveAllItems() => _slots.Where(x => x.Item != null).Select(x => x.Item).ToList();

        public void TakeAllItems(List<Item?> items)
        {
            if (items.Count > 0)
                items.ForEach(AddItem!);
        }

        public void Clear()
        {
            foreach (var item in _slots)
            {
                item.Clear();
            }
        }
    }
}
