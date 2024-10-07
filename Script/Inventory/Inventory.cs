namespace Playground.Script.Inventory
{
    using Godot;
    using Godot.Collections;
    using Playground.Script.Items;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Inventory : Node
    {
        #region
        //    public event Action<int, Item> OnItemChanged;

        //    private Item[] _items;

        //    public Inventory(int size)
        //    {
        //        if (size <= 0)
        //        {
        //            throw new ArgumentException("Inventory size must be greater than zero.");
        //        }

        //        _items = new Item[size];
        //    }

        //    /// <summary>
        //    /// Remove all items from the inventory.
        //    /// </summary>
        //    public void Clear()
        //    {
        //        for (int i = 0; i < _items.Length; i++)
        //        {
        //            RemoveItem(i);
        //        }
        //    }

        //    /// <summary>
        //    /// Returns all non-empty <see cref="Item"/>'s from this inventory.
        //    /// </summary>

        //    public IEnumerable<Item> GetItem()
        //    {
        //        return _items.Where(item => item != null);
        //    }

        //    public void MoveItemTo(Inventory other, int fromIndex, int toIndex)
        //    {
        //        ItemTransfer(this, other, fromIndex, toIndex);
        //    }

        //    public void TakeItemFrom(Inventory other, int fromIndex, int toIndex)
        //    {
        //        ItemTransfer(other, this, fromIndex, toIndex);
        //    }

        //    public void TakePartOfItemFrom(Inventory other, int fromIndex, int toIndex, int count)
        //    {
        //        PartOfItemTransfer(other, this, fromIndex, toIndex, count);
        //    }

        //    public void MovePartOfItemTo(Inventory other, int fromIndex, int toIndex, int count)
        //    {
        //        PartOfItemTransfer(this, other, fromIndex, toIndex, count);
        //    }

        //    public void SetItem(int index, Item item)
        //    {
        //        ThrowIfIndexOutOfRange(index);

        //        _items[index] = item;

        //        NotifyItemChanged(index, item);
        //    }

        //    public void AddItem(Item item)
        //    {
        //        // Try to place the item in the first empty slot
        //        if (TryFindFirstEmptySlot(out int index))
        //        {
        //            _items[index] = item;
        //            NotifyItemChanged(index, item);
        //        }
        //        else
        //        {
        //            GD.Print("Inventory is full.");
        //        }
        //    }

        //    public void RemoveItem(int index)
        //    {
        //        ThrowIfIndexOutOfRange(index);

        //        _items[index] = null;

        //        NotifyItemChanged(index, null);
        //    }

        //    public void SwapItems(int index1, int index2)
        //    {
        //        ThrowIfIndexOutOfRange(index1);
        //        ThrowIfIndexOutOfRange(index2);

        //        (_items[index2], _items[index1]) = (_items[index1], _items[index2]);

        //        NotifyItemStacksChanged(index1, _items[index1]);
        //        NotifyItemStacksChanged(index2, _items[index2]);
        //    }

        //    public bool HasItem(int index)
        //    {
        //        ThrowIfIndexOutOfRange(index);

        //        return _items[index] != null;
        //    }

        //    public Item GetItem(int index)
        //    {
        //        ThrowIfIndexOutOfRange(index);

        //        return _items[index];
        //    }

        //    public int GetItemSlotCount()
        //    {
        //        return _items.Length;
        //    }

        //    public bool GetEmptySlot()
        //    {
        //        for (int i = 0; i < _items.Length; i++)
        //        {
        //            if (_items[i] == null)
        //            {
        //                return true;
        //            }
        //        }
        //        return false;
        //    }

        //    public void DebugPrintInventory()
        //    {
        //        GD.Print(GetType().Name);

        //        for (int i = 0; i < _items.Length; i++)
        //        {
        //            GD.Print($"Slot {i}: {(_items[i] != null ? _items[i].ToString() : "Empty")}");
        //        }
        //    }

        //    public override string ToString()
        //    {
        //        return string.Join(' ', _items.Where(item => item != null));
        //    }

        //    private bool TryFindFirstEmptySlot(out int index)
        //    {
        //        for (int i = 0; i < _items.Length; i++)
        //        {
        //            if (_items[i] == null)
        //            {
        //                index = i;
        //                return true;
        //            }
        //        }

        //        index = -1;

        //        return false;
        //    }

        //    private void NotifyItemStacksChanged(int index, Item item)
        //    {
        //        OnItemChanged?.Invoke(index, item);
        //    }

        //    private void NotifyItemChanged(int index, Item item)
        //    {
        //        OnItemChanged?.Invoke(index, item);
        //    }

        //    private void PartOfItemTransfer(Inventory source, Inventory destination, int fromIndex, int toIndex, int count)
        //    {
        //        Item sourceItem = source.GetItem(fromIndex);
        //        Item destinationItem = destination.GetItem(toIndex);

        //        if (sourceItem == null)
        //        {
        //            return;
        //        }

        //        if (count <= 0 || count > sourceItem.Quantity)
        //        {
        //            throw new Exception("Invalid count for transfer.");
        //        }

        //        if (destinationItem == null)
        //        {
        //            // Destination slot is empty, create a new ItemStack with the specified count
        //            destinationItem = new Item(sourceItem.ItemName, sourceItem.Rarity, sourceItem.ItemResourcePath, sourceItem.Icon, sourceItem.MaxStackSize, sourceItem.Quantity);
        //            destination.SetItem(toIndex, destinationItem);

        //            // Remove the transferred count from the source item
        //            sourceItem.Remove(count);

        //            if (sourceItem.Quantity == 0)
        //            {
        //                source.RemoveItem(fromIndex);
        //            }
        //            else
        //            {
        //                source.NotifyItemStacksChanged(fromIndex, sourceItem);
        //            }
        //        }
        //        else if (destinationItem.ItemResourcePath.Equals(sourceItem.ItemResourcePath))
        //        {
        //            // Destination item is of the same material, add the count to it
        //            destinationItem.Add(count);
        //            destination.NotifyItemStacksChanged(toIndex, destinationItem);

        //            // Remove the transferred count from the source item
        //            sourceItem.Remove(count);

        //            if (sourceItem.Quantity == 0)
        //            {
        //                source.RemoveItem(fromIndex);
        //            }
        //            else
        //            {
        //                source.NotifyItemStacksChanged(fromIndex, sourceItem);
        //            }
        //        }
        //        else
        //        {
        //            // No way to tell if the user was drag right clicking or doing a single right
        //            // click. Usually we would swap items here but instead lets do nothing.
        //        }
        //    }

        //    private void ItemTransfer(Inventory source, Inventory destination, int fromIndex, int toIndex)
        //    {
        //        Item sourceItem = source.GetItem(fromIndex);
        //        Item destinationItem = destination.GetItem(toIndex);

        //        if (sourceItem != null && destinationItem != null)
        //        {
        //            if (sourceItem.ItemResourcePath.Equals(destinationItem.ItemResourcePath))
        //            {
        //                // Stack items
        //                destinationItem.Add(sourceItem.Quantity);
        //                destination.NotifyItemStacksChanged(toIndex, destinationItem);

        //                source.RemoveItem(fromIndex);
        //                return;
        //            }
        //            else
        //            {
        //                // Swap items
        //                destination.SetItem(toIndex, sourceItem);
        //                source.SetItem(fromIndex, destinationItem);
        //                return;
        //            }
        //        }

        //        // Place or Pickup items
        //        destination.SetItem(toIndex, sourceItem);
        //        source.RemoveItem(fromIndex);
        //    }

        //    private void ThrowIfIndexOutOfRange(int index)
        //    {
        //        if (index < 0 || index >= _items.Length)
        //        {
        //            throw new IndexOutOfRangeException("Index out of range.");
        //        }
        //    }

        //    private bool TryStackItemFullSearch(Item item)
        //    {
        //        for (int i = 0; i < _items.Length; i++)
        //        {
        //            if (_items[i] != null && _items[i].ItemResourcePath.Equals(item.ItemResourcePath))
        //            {
        //                _items[i].Add(item.Quantity);
        //                NotifyItemStacksChanged(i, _items[i]);
        //                return true;
        //            }
        //        }

        //        return false;
        //    }
        //}
        #endregion

        private string _itemSlotPath = "res://Node/InventorySlot.tscn";
        private GridContainer _inventoryContainer;
        private static Inventory instance = null;
        private List<InventorySlot> _slots = [];
        private PackedScene _inventorySlot;
        [Export]
        private Array<Item> _starterItems;
        private Panel _inventoryWindow;
        private int _capacity = 35;
        private Label _infoText;
        public Label InfoText
        {
            get => _infoText;
            private set => _infoText = value;
        }

        private static Inventory Instance
        {
            get
            {
                instance ??= new Inventory();
                return instance;
            }
        }

        public override void _Ready()
        {
            _inventoryWindow = GetNode<Panel>("InventoryWindow");
            _inventoryContainer = GetNode<GridContainer>("InventoryWindow/InventoryContainer");
            _infoText = GetNode<Label>("InventoryWindow/InfoText");
            _inventorySlot = ResourceLoader.Load<PackedScene>(_itemSlotPath);
            Initialize();
            ToggleWindow(false);

            foreach (InventorySlot child in _inventoryContainer.GetChildren())
            {
                _slots.Append(child);
                child.SetItem(null);
                child._inventory = this;
            }

            foreach (Item item in _starterItems)
            {
                AddItem(item);
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressed("Inventory"))
            {
                ToggleWindow(!_inventoryWindow.Visible);
            }
        }

        public void Initialize()
        {
            for (int i = 0; i < _capacity; i++)
            {
                InventorySlot inventorySlot = _inventorySlot.Instantiate<InventorySlot>();
                _inventoryContainer.AddChild(inventorySlot);
                _slots.Add(inventorySlot);
            }
        }

        public bool ToggleWindow(bool isOpen)
        {
            #region if you need to hide the mouse cursor
            //if (isOpen)
            //{
            //    Input.MouseMode = Input.MouseModeEnum.Visible;
            //}
            //else
            //{
            //    Input.MouseMode = Input.MouseModeEnum.Captured;
            //}

            #endregion
            return _inventoryWindow.Visible = isOpen;
        }

        public void OnGivePlayerItem(Item item, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                AddItem(item);
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

        public void RemoveItem(Item item)
        {
            var slot = GetSlotToRemove(item);

            if (slot == null || slot.InventoryItem == item)
            {
                GD.Print("No slot available.");
                return;
            }

            slot.RemoveItem(item);
        }

        public InventorySlot GetSlotToAdd(Item item)
        {
            foreach (InventorySlot slot in _slots)
            {
                if (slot.InventoryItem == null || (slot.InventoryItem.Quantity < item.MaxStackSize && slot.InventoryItem.Guid == item.Guid))
                {
                    return slot;
                }
            }

            return null;
        }

        public InventorySlot GetSlotToRemove(Item item)
        {
            return _slots.Find(slot => slot.InventoryItem.Guid == item.Guid);
        }

        public int GetNumberOfItems(Item item)
        {
            return _slots.FindAll(slot => slot.InventoryItem.Guid == item.Guid).Count;
        }
    }
}
