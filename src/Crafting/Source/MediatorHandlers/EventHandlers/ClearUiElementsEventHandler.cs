namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Crafting.Source;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Events;

    public class ClearUiElementsEventHandler : IEventHandler<ClearUiElementsEvent>
    {
        private readonly UIElementProvider _uIElementProvider;

        public ClearUiElementsEventHandler(UIElementProvider provider)
        {
            _uIElementProvider = provider;
        }

        public void Handle(ClearUiElementsEvent evnt) => _uIElementProvider.ClearSource(evnt.Source);
    }
}
