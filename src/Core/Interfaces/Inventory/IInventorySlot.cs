namespace Core.Interfaces.Inventory
{
    using System;
    using Enums;
    using Items;

    public interface IInventorySlot : IMouseExitable
    {
        int Quantity { get; set; }
        ItemInstance? CurrentItem { get; }
        event Action<IInventorySlot, MouseInteractions>? ItemInteraction;
        bool TryAddStacks(int amount, out int leftover);
        void SetItem(ItemInstance item, int amount = 1);
        void ClearSlot(bool isDeleted = false);
        bool TryRemoveItemStacks( int amount = 1);
        bool HaveThisItem(ItemInstance instance);
    }
}
