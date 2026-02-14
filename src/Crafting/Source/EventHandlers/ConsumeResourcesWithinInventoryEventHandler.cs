namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;

    public class ConsumeResourcesWithinInventoryEventHandler(IInventory inventory, IGameMessageBus uiGameMessageBus)
        : IEventHandler<ConsumeResourcesInInventoryEvent>
    {
        public Task HandleAsync(ConsumeResourcesInInventoryEvent evnt)
        {
            foreach (var res in evnt.Resources)
                inventory.RemoveItemById(res.Key, res.Value);
            return Task.CompletedTask;
        }
    }
}
