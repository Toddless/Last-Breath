namespace Core.Inventory
{
    using System;
    using System.Collections.Generic;
    using Items;

    public interface IInventory
    {
        int InventoryCapacity { get; }

        event Action<string, int>? ItemAmountChanges;
        event Action<string, string, int, int>? InventoryFull;
        event Action<string>? NotEnoughItems;

        List<string> GetAllItemIdsWithTag(string tag);
        T? GetItem<T>(string instanceId) where T : class, IItem;
        int GetTotalItemAmount(string id);
        bool TryAddItem(IItem item, int amount = 1);
        int GetAvailableCapacity();
        void RemoveItemById(string itemId, int amount = 1);
        void RemoveItemByInstanceId(string instanceId);
        bool TryAddItemStacks(string itemId, int amount = 1);
    }
}
