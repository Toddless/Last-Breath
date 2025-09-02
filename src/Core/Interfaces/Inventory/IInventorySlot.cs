namespace Core.Interfaces.Inventory
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Items;

    public interface IInventorySlot
    {
        int Quantity { get; set; }
        IItem? CurrentItem { get; }

        event Action<IItem, MouseButtonPressed>? OnItemClicked;

        int AddItemStacks(int amount);
        void AddNewItem(IItem item, int amount = 1);
        void ClearSlot();
        bool RemoveItemStacks(int amount);
    }
}
