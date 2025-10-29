namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Events;

    public class ClearUiElementsEventHandler : IEventHandler<ClearUiElementsEvent>
    {
        private readonly IUIElementProvider _uIElementProvider;

        public ClearUiElementsEventHandler(IUIElementProvider provider)
        {
            _uIElementProvider = provider;
        }

        public void Handle(ClearUiElementsEvent evnt) => _uIElementProvider.ClearSource(evnt.Source);
    }
}
