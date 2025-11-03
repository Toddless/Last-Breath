namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using System;
    using Utilities;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Mediator.Events;

    public class OpenCraftingWindowEventHandler : IEventHandler<OpenCraftingWindowEvent>
    {
        private readonly IUIElementProvider _uiElementProvider;
        private readonly IInventory _inventory;

        public OpenCraftingWindowEventHandler(IUIElementProvider provider, IInventory inventory)
        {
            _uiElementProvider = provider;
            _inventory = inventory;
        }

        public void Handle(OpenCraftingWindowEvent evnt)
        {
            try
            {
                if (!_uiElementProvider.IsInstanceTypeExist(typeof(CraftingWindow), out var exist))
                    _uiElementProvider.CreateAndShowWindowElement<CraftingWindow>();
                else
                {
                    ArgumentNullException.ThrowIfNull(exist);
                    if (exist.IsVisibleInTree())
                        _uiElementProvider.HideWindowElement<CraftingWindow>();
                    else
                        _uiElementProvider.ShowWindowElement<CraftingWindow>();
                }
                var craftingWindow = exist as CraftingWindow;
                if (evnt.IsItem) craftingWindow?.SetItem(_inventory.GetItem<IEquipItem>(evnt.Id));
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to open crafging window", ex, this);
            }
        }
    }
}
