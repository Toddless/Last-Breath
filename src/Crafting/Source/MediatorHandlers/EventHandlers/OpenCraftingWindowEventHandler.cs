namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Crafting.Source;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Mediator.Events;

    internal class OpenCraftingWindowEventHandler : IEventHandler<OpenCraftingWindowEvent>
    {
        private readonly UIElementProvider _provider;
        private readonly IInventory _inventory;

        public OpenCraftingWindowEventHandler(UIElementProvider provider, IInventory inventory)
        {
            _provider = provider;
            _inventory = inventory;
        }

        public void Handle(OpenCraftingWindowEvent evnt)
        {
            var window = _provider.CreateSingleClosable<CraftingWindow>();
            if (evnt.IsItem) window.SetItem(_inventory.GetItem<IEquipItem>(evnt.Id));
        }
    }
}
