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

        ItemInstance? GetCurrentItemByInstanceId(string id);
        List<string> GetAllItemIdsWithTag(string tag);
        IItem? GetItemInstance(string instanceId);
        int GetTotalItemAmount(string id);
        void AddItem(IItem item, int amount = 1);
        void RemoveItemById(string itemId, int amount = 1);
        void RemoveItemByInstanceId(string instanceId);
        int ReturnItemToInventory(ItemInstance instance, int amount = 1);
        void Clear();
    }
}
