namespace Crafting.Source.MediatorHandlers
{
    using Core.Interfaces.Mediator;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Mediator.Events;
    using Core.Interfaces.Mediator.Requests;

    public class OpenWindowEventHandler : IEventHandler<OpenWindowEvent>
    {
        private readonly IUiMediator _uiMediator;

        public OpenWindowEventHandler(IUiMediator mediator)
        {
            _uiMediator = mediator;
        }

        public void Handle(OpenWindowEvent request)
        {
            var winType = request.WindowType;
            var id = request.Parameter ?? string.Empty;
            switch (true)
            {
                case var _ when winType == typeof(CraftingWindow):
                    _uiMediator.Publish(new OpenCraftingWindowEvent(id));
                    break;
                case var _ when winType == typeof(CraftingItems):
                    _uiMediator.Publish(new OpenCraftingItemsEvent(id));
                    break;
            }
        }
    }
}
