namespace Crafting.Source.EventHandlers
{
    using System;
    using Utilities;
    using UIElements;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using System.Threading.Tasks;
    using Core.Interfaces.Inventory;

    public class OpenCraftingWindowEventHandler(IUiElementProvider provider, IInventory inventory)
        : IEventHandler<OpenCraftingWindowEvent>
    {
        public Task HandleAsync(OpenCraftingWindowEvent evnt)
        {
            try
            {
                if (!provider.IsInstanceTypeExist(typeof(CraftingWindow), out var exist))
                    provider.CreateAndShowWindowElement<CraftingWindow>();
                else
                {
                    ArgumentNullException.ThrowIfNull(exist);
                    if (exist.IsVisibleInTree())
                        provider.HideWindowElement<CraftingWindow>();
                    else
                        provider.ShowWindowElement<CraftingWindow>();
                }

                var craftingWindow = exist as CraftingWindow;
                if (evnt.IsItem) craftingWindow?.SetItem(inventory.GetItem<IEquipItem>(evnt.Id));
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to open crafting window", ex, this);
            }

            return Task.CompletedTask;
        }
    }
}
