using Godot;
using Playground.Script.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Playground.Script.Inventory
{
    public partial class Inventory : GridContainer
    {
        public event Action<int, ItemStack> OnItemStacksChanged;
        public event Action<int, Item> OnItemChanged;

        private ItemStack[] _itemStacks;

        private Item[] _items;

        public Inventory(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentException("Inventory size must be greater than zero.");
            }

            _itemStacks = new ItemStack[size];
            _items = new Item[size];
        }

        /// <summary>
        /// Remove all items from the inventory.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _itemStacks.Length; i++)
            {
                RemoveItem(i);
            }
        }

        /// <summary>
        /// Returns all non-empty <see cref="ItemStack"/>'s from this inventory.
        /// </summary>
        public IEnumerable<ItemStack> GetItems()
        {
            return _itemStacks.Where(item => item != null);
        }

        public IEnumerable<Item> GetItem()
        {
            return _items.Where(item => item != null);
        }


        public void MoveItemTo(Inventory other, int fromIndex, int toIndex)
        {
            ItemTransfer(this, other, fromIndex, toIndex);
        }

        public void TakeItemFrom(Inventory other, int fromIndex, int toIndex)
        {
            ItemTransfer(other, this, fromIndex, toIndex);
        }

        public void TakePartOfItemFrom(Inventory other, int fromIndex, int toIndex, int count)
        {
            PartOfItemTransfer(other, this, fromIndex, toIndex, count);
        }

        public void MovePartOfItemTo(Inventory other, int fromIndex, int toIndex, int count)
        {
            PartOfItemTransfer(this, other, fromIndex, toIndex, count);
        }

        public void SetItemStacks(int index, ItemStack item)
        {
            ThrowIfIndexOutOfRange(index);

            _itemStacks[index] = item;

            NotifyItemStacksChanged(index, item);
        }

        public void SetItem(int index, Item item)
        {
            ThrowIfIndexOutOfRange(index);

            _items[index] = item;

            NotifyItemChanged(index, item);
        }

        public void AddItemStacks(ItemStack item)
        {
            // Try to stack the item with an existing item in the inventory
            if (TryStackItemFullSearch(item))
            {
                return;
            }

            // If the item cannot be stacked, try to place the item in the first empty slot
            if (TryFindFirstEmptySlot(out int index))
            {
                _itemStacks[index] = item;
                NotifyItemStacksChanged(index, item);
            }
            else
            {
                GD.Print("Inventory is full.");
            }
        }

        public void AddItem(Item item)
        {
            // If the item cannot be stacked, try to place the item in the first empty slot
            if (TryFindFirstEmptySlot(out int index))
            {
                _items[index] = item;
                NotifyItemChanged(index, item);
            }
            else
            {
                GD.Print("Inventory is full.");
            }
        }


        public void RemoveItem(int index)
        {
            ThrowIfIndexOutOfRange(index);

            _itemStacks[index] = null;

            NotifyItemChanged(index, null);
        }

        public void SwapItems(int index1, int index2)
        {
            ThrowIfIndexOutOfRange(index1);
            ThrowIfIndexOutOfRange(index2);

            (_itemStacks[index2], _itemStacks[index1]) = (_itemStacks[index1], _itemStacks[index2]);

            NotifyItemStacksChanged(index1, _itemStacks[index1]);
            NotifyItemStacksChanged(index2, _itemStacks[index2]);
        }

        public bool HasItem(int index)
        {
            ThrowIfIndexOutOfRange(index);

            return _itemStacks[index] != null;
        }

        public ItemStack GetItem(int index)
        {
            ThrowIfIndexOutOfRange(index);

            return _itemStacks[index];
        }

        public int GetItemSlotCount()
        {
            return _itemStacks.Length;
        }

        public void DebugPrintInventory()
        {
            GD.Print(GetType().Name);

            for (int i = 0; i < _itemStacks.Length; i++)
            {
                GD.Print($"Slot {i}: {(_itemStacks[i] != null ? _itemStacks[i].ToString() : "Empty")}");
            }
        }

        public override string ToString()
        {
            return string.Join(' ', _itemStacks.Where(item => item != null));
        }

        private void NotifyItemStacksChanged(int index, ItemStack item)
        {
            OnItemStacksChanged?.Invoke(index, item);
        }

        private void NotifyItemChanged(int index, Item item)
        {
            OnItemChanged?.Invoke(index, item);
        }


        private void PartOfItemTransfer(Inventory source, Inventory destination, int fromIndex, int toIndex, int count)
        {
            ItemStack sourceItem = source.GetItem(fromIndex);
            ItemStack destinationItem = destination.GetItem(toIndex);

            if (sourceItem == null)
            {
                return;
            }

            if (count <= 0 || count > sourceItem.Count)
            {
                throw new Exception("Invalid count for transfer.");
            }

            if (destinationItem == null)
            {
                // Destination slot is empty, create a new ItemStack with the specified count
                destinationItem = new ItemStack(sourceItem.Material, count);
                destination.SetItemStacks(toIndex, destinationItem);

                // Remove the transferred count from the source item
                sourceItem.Remove(count);

                if (sourceItem.Count == 0)
                {
                    source.RemoveItem(fromIndex);
                }
                else
                {
                    source.NotifyItemStacksChanged(fromIndex, sourceItem);
                }
            }
            else if (destinationItem.Material.Equals(sourceItem.Material))
            {
                // Destination item is of the same material, add the count to it
                destinationItem.Add(count);
                destination.NotifyItemStacksChanged(toIndex, destinationItem);

                // Remove the transferred count from the source item
                sourceItem.Remove(count);

                if (sourceItem.Count == 0)
                {
                    source.RemoveItem(fromIndex);
                }
                else
                {
                    source.NotifyItemStacksChanged(fromIndex, sourceItem);
                }
            }
            else
            {
                // No way to tell if the user was drag right clicking or doing a single right
                // click. Usually we would swap items here but instead lets do nothing.
            }
        }

        private void ItemTransfer(Inventory source, Inventory destination, int fromIndex, int toIndex)
        {
            ItemStack sourceItem = source.GetItem(fromIndex);
            ItemStack destinationItem = destination.GetItem(toIndex);

            if (sourceItem != null && destinationItem != null)
            {
                if (sourceItem.Material.Equals(destinationItem.Material))
                {
                    // Stack items
                    destinationItem.Add(sourceItem.Count);
                    destination.NotifyItemStacksChanged(toIndex, destinationItem);

                    source.RemoveItem(fromIndex);
                    return;
                }
                else
                {
                    // Swap items
                    destination.SetItemStacks(toIndex, sourceItem);
                    source.SetItemStacks(fromIndex, destinationItem);
                    return;
                }
            }

            // Place or Pickup items
            destination.SetItemStacks(toIndex, sourceItem);
            source.RemoveItem(fromIndex);
        }

        private void ThrowIfIndexOutOfRange(int index)
        {
            if (index < 0 || index >= _itemStacks.Length)
            {
                throw new IndexOutOfRangeException("Index out of range.");
            }
        }

        private bool TryStackItemFullSearch(ItemStack item)
        {
            for (int i = 0; i < _itemStacks.Length; i++)
            {
                if (_itemStacks[i] != null && _itemStacks[i].Material.Equals(item.Material))
                {
                    _itemStacks[i].Add(item.Count);
                    NotifyItemStacksChanged(i, _itemStacks[i]);
                    return true;
                }
            }

            return false;
        }


        private bool TryFindFirstEmptySlot(out int index)
        {
            for (int i = 0; i < _itemStacks.Length; i++)
            {
                if (_itemStacks[i] == null)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;

            return false;
        }
    }
}
