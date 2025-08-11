namespace Core.Interfaces.Inventory
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Items;
    using Godot;

    public interface IInventory
    {
        event Action? InventoryFull;
        event Action<IItem, MouseButtonPressed, IInventory>? ItemSlotClicked;
        event Action? NotEnougthItems;

        void AddItem(IItem item);
        void Clear();
        int GetNumberOfItems(IItem item);
        List<IItem?> GiveAllItems();
        void Initialize(int size, GridContainer container);
        void RemoveItem(string itemId, int amount = 1);
        void TakeAllItems(List<IItem?> items);
    }
}
