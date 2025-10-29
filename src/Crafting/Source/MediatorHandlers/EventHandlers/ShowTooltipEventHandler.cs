namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Crafting.Source;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Events;
    using Crafting.Source.UIElements;

    internal class ShowTooltipEventHandler : IEventHandler<ShowInventorySlotButtonsTooltipEvent>
    {
        private readonly UIElementProvider _uiElementProvider;

        public ShowTooltipEventHandler(UIElementProvider uIElementProvider)
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
