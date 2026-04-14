namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;

    public class ConsumeResourcesWithinInventoryMessageHandler(IInventory inventory)
        : IMessageHandler<ConsumeResourcesInInventoryMessage>
    {
        public Task HandleMessageAsync(ConsumeResourcesInInventoryMessage message)
        {
            foreach ((string id, int amount) in message.Resources)
                inventory.RemoveItemById(id, amount);
            return Task.CompletedTask;
        }
    }
}
