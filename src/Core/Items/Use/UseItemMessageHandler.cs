namespace Core.Items.Use
{
    using System.Threading.Tasks;
    using Inventory;
    using MessageBus;
    using MessageBus.Messages;

    /// <summary>The one channel for using a bag item: resolve the item, dispatch to its behavior.
    /// No behavior claiming the item = silent no-op (the UI would not have shown the button).
    /// The behavior itself stays the gate — this handler never pre-checks CanUse.</summary>
    public class UseItemMessageHandler(IInventory inventory, IItemUseService useService)
        : IMessageHandler<UseItemMessage>
    {
        public Task HandleMessageAsync(UseItemMessage message)
        {
            if (inventory.GetItem<IItem>(message.ItemInstanceId) is { } item)
                useService.BehaviorFor(item)?.Use(item);
            return Task.CompletedTask;
        }
    }
}
