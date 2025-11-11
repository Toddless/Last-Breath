namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator;

    public class ConsumeResourcesWithinInventoryEventHandler(IInventory inventory, IMediator uiMediator)
        : IEventHandler<ConsumeResourcesInInventoryEvent>
    {
        public Task HandleAsync(ConsumeResourcesInInventoryEvent evnt)
        {
            foreach (var res in evnt.Resources)
                inventory.RemoveItemById(res.Key, res.Value);
            uiMediator.RaiseUpdateUi();
            return Task.CompletedTask;
        }
    }
}
