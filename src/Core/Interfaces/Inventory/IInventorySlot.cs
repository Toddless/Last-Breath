namespace Core.Interfaces.Inventory
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Items;
    using Godot;

    public interface IInventorySlot
    {
        int Quantity { get; set; }

        event Action<IItem, MouseButtonPressed>? OnItemClicked;

        static abstract PackedScene Initialize();
        int AddItemStacks(int amount);
        void AddNewItem(IItem item);
        void ClearSlot();
        bool RemoveItemStacks(int amount);
    }
}
