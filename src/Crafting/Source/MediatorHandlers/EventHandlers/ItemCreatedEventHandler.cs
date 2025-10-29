namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Crafting.Source;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator.Events;

    internal class ItemCreatedEventHandler : IEventHandler<ItemCreatedEvent>
    {
        private readonly UIElementProvider _uIElementProvider;
        private readonly ISystemMediator _systemMediator;
        private readonly IInventory _inventory;

        public ItemCreatedEventHandler(UIElementProvider provider, IInventory inventory, ISystemMediator systemMediator)
        {
            _uIElementProvider = provider;
            _inventory = inventory;
            _systemMediator = systemMediator;
        }

        public void Handle(ItemCreatedEvent evnt)
        {
            var notifier = _uIElementProvider.CreateItemCreatedNotifier(evnt.CreatedItem);
            _inventory.TryAddItem(evnt.CreatedItem);

            notifier.DestroyPressed += OnDestroyPressed;

            void OnDestroyPressed()
            {
                _systemMediator.Publish(new DestroyItemEvent(evnt.CreatedItem.InstanceId));
                notifier.DestroyPressed -= OnDestroyPressed;
            }
        }
    }
}

