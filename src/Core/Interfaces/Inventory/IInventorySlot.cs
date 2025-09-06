namespace Core.Interfaces.Inventory
{
    using System;
    using Core.Enums;

    public interface IInventorySlot
    {
        int Quantity { get; set; }
        string? CurrentItem { get; }

        event Action<string, MouseButtonPressed>? OnItemClicked;
        int AddItemStacks(int amount);
        void SetItem(string item, int amount = 1);
        void ClearSlot(bool isDeleted = false);
        bool RemoveItemStacks(int amount);
        bool ItemHasTag(string tag);
    }
}
