namespace Core.Interfaces.Inventory
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Items;

    public interface IInventory
    {
        event Action<string, MouseButtonPressed, IInventory>? ItemSlotClicked;
        event Action<string, int>? ItemAmountChanges;
        event Action<string, string, int, int>? InventoryFull;
        event Action<string>? NotEnougthItems;

        ItemInstance? GetItemInstance(string id);
        List<string> GetAllItemIdsWithTag(string tag);
        IItem? GetItem(string instanceId);
        int GetTotalItemAmount(string id);
        bool TryAddItem(IItem item, int amount = 1);
        void RemoveItemById(string itemId, int amount = 1);
        void RemoveItemByInstanceId(string instanceId);
        bool TryReturnItemInstanceToInventory(ItemInstance instance, int amount = 1);
        void Clear();
    }
}
