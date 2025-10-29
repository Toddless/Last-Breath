namespace Crafting.Services.DI
{
    using System;
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;

    internal class UIMediator : IUiMediator
    {
        public event Action? UpdateUi;

        public async Task<TResponce> Send<TRequest, TResponce>(TRequest request)
           where TRequest : IRequest<TResponce>
        {
            var handler = ServiceProvider.Instance.GetService<IRequestHandler<TRequest, TResponce>>();
            return await handler.Handle(request);
        }

        public void Publish<TEvent>(TEvent evt)
            where TEvent : IEvent
        {
            var handlers = ServiceProvider.Instance.GetServices<IEventHandler<TEvent>>();
            foreach (var handler in handlers)
                handler.Handle(evt);
        }

        public void RaiseUpdateUi() => UpdateUi?.Invoke();
    }
}
