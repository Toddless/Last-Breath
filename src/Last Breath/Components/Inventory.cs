namespace Playground.Components
{
    using Godot;
    using Playground.Script.Inventory;
    using Playground.Script.Items;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Inventory 
    {
        private List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;
        private Action? _showInventory, _hideInventory;

        //public Action? ShowInventory
        //{
        //    get => _showInventory;
        //    set => _showInventory = value;
        //}

        //public Action? HideInventory
        //{
        //    get => _hideInventory;
        //    set => _hideInventory = value;
        //}

        public void Initialize(int size, string path, GridContainer container/*, Action? hideInventory, Action? showInventory*/)
        {
            _inventorySlot = ResourceLoader.Load<PackedScene>(path);
            for (int i = 0; i < size; i++)
            {
                InventorySlot inventorySlot = _inventorySlot!.Instantiate<InventorySlot>();
                container.AddChild(inventorySlot);
                _slots.Add(inventorySlot);
            }
            //_showInventory = showInventory;
            //_hideInventory = hideInventory;
        }

        public void AddItem(Item item)
        {
            var slot = GetSlotToAdd(item);
            if (slot == null)
            {
                return;
            }

            if (slot.Item == null)
            {
                slot.SetItem(item);
            }
            else if (slot.Item.Guid == item.Guid)
            {
                slot.AddItem(item);
            }
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

        public InventorySlot? GetSlotToAdd(Item item)
        {
            return _slots.FirstOrDefault(itemSlot => itemSlot.Item == null || (itemSlot.Item.Guid == item.Guid && itemSlot.Item.MaxStackSize >= item.Quantity));
        }

        public InventorySlot? GetSlotToRemove(Item? item)
        {
            // this method work correctly without first condition only if i equip or remove an item from right to left
            // cause if an item is removed from left to right, then after first cycle method return null
            // and NullReferenceException is thrown
            return _slots.FirstOrDefault(x => x.Item != null && x.Item.Guid == item?.Guid);
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
    }
}
