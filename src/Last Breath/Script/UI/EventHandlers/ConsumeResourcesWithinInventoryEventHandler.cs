namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator.Events;

    public class ConsumeResourcesWithinInventoryEventHandler : IEventHandler<ConsumeResourcesInInventoryEvent>
    {
        private readonly IInventory _inventory;
        private readonly IUiMediator _uiMediator;

        public ConsumeResourcesWithinInventoryEventHandler(IInventory inventory, IUiMediator uiMediator)
        {
            _inventory = inventory;
            _uiMediator = uiMediator;
        }

        public void Handle(ConsumeResourcesInInventoryEvent evnt)
        {
            foreach (var res in evnt.Resources)
                _inventory.RemoveItemById(res.Key, res.Value);
            _uiMediator.RaiseUpdateUi();
        }
    }
}
