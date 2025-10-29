namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Crafting.Source;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator.Events;
    using Core.Interfaces.Items;

    internal class ShowInventoryItemEventHandler : IEventHandler<ShowInventoryItemEvent>
    {
        private readonly IInventory _inventory;
        private readonly UIElementProvider _uiElementProvider;

        public ShowInventoryItemEventHandler(IInventory inventory, UIElementProvider uiElementProvider)
        {
            _inventory = inventory;
            _uiElementProvider = uiElementProvider;
        }

        public void Handle(ShowInventoryItemEvent evnt)
        {
            var item = _inventory.GetItem<IItem>(evnt.Item.InstanceId);
            if (item != null)
                _uiElementProvider.CreateItemDetails(item, evnt.Source);
        }
    }
}
