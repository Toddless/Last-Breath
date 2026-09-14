namespace Core.Inventory
{
    using System;
    using Items;

    /// <summary>Quiet partial acceptance with deferred notifications for cross-container commits.</summary>
    public interface IInventoryTransfer
    {
        IDisposable DeferNotifications();
        int ReceiveUpTo(IItem item, int amount);
    }
}
