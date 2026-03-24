namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;

    public class ConsumeResourcesWithinInventoryMessageHandler(IInventory inventory, IGameMessageBus uiGameMessageBus)
        : IMessageHandler<ConsumeResourcesInInventoryMessage>
    {
        public Task HandleMessageAsync(ConsumeResourcesInInventoryMessage message)
        {
            foreach (var res in message.Resources)
                inventory.RemoveItemById(res.Key, res.Value);
            return Task.CompletedTask;
        }
    }
}
