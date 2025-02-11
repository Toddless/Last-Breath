namespace Playground.Script.Enemy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public class EnemyInventory : IInventory
    {
        private List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;
        private Action? _showInventory;
        private Action? _hideInventory;

        public Action? ShowInventory
        {
            get => _showInventory;
            set => _showInventory = value;
        }
        public Action? HideInventory
        {
            get => _hideInventory;
            set => _hideInventory = value;
        }

        public void Initialize(int size, string path, GridContainer container, Action? hideInventory, Action? showInventory)
        {
            _inventorySlot = ResourceLoader.Load<PackedScene>(path);
            for (int i = 0; i < size; i++)
            {
                InventorySlot inventorySlot = _inventorySlot.Instantiate<InventorySlot>();
                container.AddChild(inventorySlot);
                _slots.Add(inventorySlot);
            }
            _hideInventory = hideInventory;
            _showInventory = showInventory;
        }

        public List<Item> GivePlayerItems() => _slots.Where(x => x.Item != null).Select(x => x.Item!).ToList();

        public void AddItem(Item? item)
        {
            if (item == null)
                return;
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

        public List<Item?> GiveAllItems()
        {
            var itemsToGive = _slots.Where(x => x.Item != null).Select(x => x.Item).ToList();
            _slots.ForEach(x => RemoveItem(x.Item));
            return itemsToGive;
        }

        public void TakeAllItems(List<Item?> items)
        {
            if (items.Count > 0)
                items.ForEach(AddItem!);
        }

        private void RemoveItem(Item? item)
        {
            var slot = GetSlotToRemove(item);

            if (slot == null)
            {
                return;
            }
            slot.RemoveItem(item);
        }

        private InventorySlot? GetSlotToAdd(Item newItem)
        {
            return _slots.FirstOrDefault(itemInSlot => itemInSlot.Item == null || itemInSlot.Item.Guid == newItem.Guid && itemInSlot.Item.Quantity < newItem.MaxStackSize);
        }

        private InventorySlot? GetSlotToRemove(Item? item)
        {
            // this method work correctly without first condition only if i equip or remove an item from right to left
            // cause if an item is removed from left to right, then after first cycle method return null
            // and NullReferenceException is thrown
            return _slots.FirstOrDefault(x => x.Item != null && x.Item.Guid == item?.Guid);
        }
    }
}
