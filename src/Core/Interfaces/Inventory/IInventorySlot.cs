namespace Core.Interfaces.Inventory
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Items;

    public interface IInventorySlot
    {
        int Quantity { get; set; }
        ItemInstance? CurrentItem { get; }
        event Action<string, MouseButtonPressed>? OnItemClicked;
        bool TryAddStacks(int amount, out int leftover);
        void SetItem(ItemInstance item, int amount = 1);
        void ClearSlot(bool isDeleted = false);
        bool TryRemoveItemStacks( int amount = 1);
        bool HaveThisItem(ItemInstance instance);
    }
}
