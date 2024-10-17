namespace Playground.Script.Inventory
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Items;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [GlobalClass]
    public partial class InventoryComponent : Node
    {
        #region Path conts
        private const string InventoryContainer = "/root/MainScene/CharacterBody2D/InventoryComponent/InventoryWindow/InventoryContainer";
        private const string ClearButton = "/root/MainScene/CharacterBody2D/InventoryComponent/InventoryWindow/ClearButton";
        private const string InventoryWindow = "/root/MainScene/CharacterBody2D/InventoryComponent/InventoryWindow";
        #endregion

        #region Private fields
        private string _inventorySlotPath = SceneParh.InventorySlot;
        private GridContainer? _inventoryContainer;
        private List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;
        private Panel? _inventoryWindow;
        private Button? _clearButton;
        private Item? _item;
        [Export]
        private int _capacity;
        #endregion

        public override void _Ready()
        {
            _inventorySlot = ResourceLoader.Load<PackedScene>(_inventorySlotPath);
            _inventoryContainer = GetNode<GridContainer>(InventoryContainer);
            _clearButton = GetNode<Button>(ClearButton);
            _inventoryWindow = GetNode<Panel>(InventoryWindow);
            _clearButton.Pressed += OnClearButtonPressed;
            if(_inventorySlot == null || _inventoryWindow == null || _inventoryContainer == null)
            {
                ArgumentNullException.ThrowIfNull(_inventorySlot);
                ArgumentNullException.ThrowIfNull(_inventoryWindow);
                ArgumentNullException.ThrowIfNull(_inventoryContainer);
            }
            Initialize();
            ToggleWindow(false);
        }

        public void Initialize()
        {
            for (int i = 0; i < _capacity; i++)
            {
                InventorySlot inventorySlot = _inventorySlot!.Instantiate<InventorySlot>();
                _inventoryContainer!.AddChild(inventorySlot);
                _slots.Add(inventorySlot);
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressed(InputMaps.OpenInventoryOnI))
            {
                ToggleWindow(!_inventoryWindow!.Visible);
                return;
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
            return _inventoryWindow!.Visible = isOpen;
        }

        public void OnGivePlayerItem(Item item, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                AddItem(item);
            }
        }

        private void OnClearButtonPressed()
        {
            RemoveAllItemsFromInventory();
        }

        private void RemoveAllItemsFromInventory()
        {
            foreach (InventorySlot child in _inventoryContainer!.GetChildren().Cast<InventorySlot>())
            {
                child.RemoveItself();
            }
        }

        private void RemoveAllItemsFromSlot()
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
                GD.Print("not found. Returned null");
                return;
            }

            slot.RemoveItem(item);
        }

        public InventorySlot? GetSlotToAdd(Item item)
        {
            foreach (InventorySlot slot in _slots)
            {
                if (slot.InventoryItem == null || (slot.InventoryItem.Guid == item.Guid && slot.InventoryItem.Quantity < item.MaxStackSize))
                {
                    return slot;
                }
            }

            return null;
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
