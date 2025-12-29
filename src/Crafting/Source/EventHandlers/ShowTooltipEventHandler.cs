namespace Crafting.Source.EventHandlers
{
    using UIElements;
    using Core.Interfaces.Data;
    using Core.Interfaces.Events;
    using System.Threading.Tasks;

    public class ShowTooltipEventHandler(IUiElementProvider uIElementProvider)
        : IEventHandler<ShowInventorySlotButtonsTooltipEvent>
    {
        public Task HandleAsync(ShowInventorySlotButtonsTooltipEvent evnt)
        {
            var source = evnt.Source;
            string itemId = evnt.ItemInstanceId;
            var tooltip = uIElementProvider.CreateClosableForSource<InventorySlotTooltipButtons>(source);
            tooltip.SetItemInstanceId(itemId);
            return Task.CompletedTask;
        }
    }
}
