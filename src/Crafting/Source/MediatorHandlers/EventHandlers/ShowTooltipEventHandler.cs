namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Mediator.Events;

    public class ShowTooltipEventHandler : IEventHandler<ShowInventorySlotButtonsTooltipEvent>
    {
        private readonly IUIElementProvider _uiElementProvider;

        public ShowTooltipEventHandler(IUIElementProvider uIElementProvider)
        {
            _uiElementProvider = uIElementProvider;
        }

        public void Handle(ShowInventorySlotButtonsTooltipEvent evnt)
        {
            var source = evnt.Source;
            var itemId = evnt.ItemInstanceId;
            var tooltip = _uiElementProvider.CreateClosableForSource<InventorySlotTooltipButtons>(source);
            tooltip.SetItemInstanceId(itemId);
        }
    }
}
