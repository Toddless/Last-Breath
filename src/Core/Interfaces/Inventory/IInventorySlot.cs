namespace Core.Interfaces.Inventory
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Items;

    public interface IInventorySlot
    {
        int Quantity { get; set; }
        ItemInstance? CurrentItem { get; }
        Func<string, IItem?>? GetItemInstance { get; }
        event Action<string, MouseButtonPressed>? OnItemClicked;
        int AddItemStacks(int amount);
        void SetItem(ItemInstance item, int amount = 1);
        void ClearSlot(bool isDeleted = false);
        bool RemoveItemStacks(int amount);
    }
}
