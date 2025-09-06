namespace Core.Interfaces.Inventory
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Items;

    public interface IInventory
    {
        event Action<string, MouseButtonPressed, IInventory>? ItemSlotClicked;
        event Action? InventoryFull, NotEnougthItems;
        event Action<string, int>? ItemAmountChanges;

        IInventorySlot? GetSlotWithItemOrNull(string id);
        List<string> GetAllItemIdsWithTag(string tag);
        void AddItem(IItem item, int amount = 1);
        void RemoveItem(string itemId, int amount = 1);
        void Clear();
    }
}
