namespace Playground.Script.Inventory
{
    using Godot;
    using Playground.Script.Items;
    using System.Collections.Generic;
    using System.Linq;

    [GlobalClass]
    public partial class InventoryComponent : Node
    {
        private List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;
        private int _capacity;
        private Item? _item;

        public void Inititalize(int size, string path, GridContainer container)
        {
            _inventorySlot = ResourceLoader.Load<PackedScene>(path);
            for (int i = 0; i < size; i++)
            {
                InventorySlot inventorySlot = _inventorySlot!.Instantiate<InventorySlot>();
                container.AddChild(inventorySlot);
                _slots.Add(inventorySlot);
            }
        }

        public void OnGivePlayerItem(Item item, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                AddItem(item);
            }
        }

        public Item? GivePlayerItem()
        {
            foreach (var item in _slots)
            {
                if (item.InventoryItem != null)
                {
                    return item.InventoryItem;
                }
                break;
            }
            return null;
        }

        public void RemoveAllItemsFromSlot()
        {
            foreach (var item in _slots)
            {
                RemoveItem(item.InventoryItem);
            }
        }

        public void AddItem(Item item)
        {
            var slot = GetSlotToAdd(item);
            if (slot == null)
            {
                GD.Print("No slot available.");
                return;
            }

            if (slot.InventoryItem == null)
            {
                slot.SetItem(item);
            }
            else if (slot.InventoryItem.Guid == item.Guid)
            {
                slot.AddItem(item);
            }
        }

        public void RemoveItem(Item? item)
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
            return _slots.FirstOrDefault(x => x.InventoryItem == null || (x.InventoryItem.Guid == item.Guid && x.InventoryItem.Quantity < item.MaxStackSize));
        }

        public InventorySlot? GetSlotToRemove(Item? item)
        {
            // this method work correctly without first condition only if i equip or remove an item from right to left
            // cause if an item is removed from left to right, then after first cycle method return null
            // and NullReferenceException is thrown
            return _slots.FirstOrDefault(x => x.InventoryItem != null && x.InventoryItem.Guid == item?.Guid);
        }

        public int GetNumberOfItems(Item item)
        {
            return _slots.FindAll(slot => slot.InventoryItem != null && slot.InventoryItem.Guid == item.Guid).Count;
        }
    }
}
