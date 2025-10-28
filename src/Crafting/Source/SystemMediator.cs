namespace Crafting.Source
{
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;
    using Crafting.Source.DI;

    public class SystemMediator : ISystemMediator
    {
        public void Publish<TEvent>(TEvent evt)
            where TEvent : IEvent
        {
            var eventHandler = ServiceProvider.Instance.GetServices<IEventHandler<TEvent>>();
            foreach (var handler in eventHandler)
                handler?.Handle(evt);
        }

        public async Task<TResponce> Send<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>
        {
            var handler = ServiceProvider.Instance.GetService<IRequestHandler<TRequest, TResponce>>();
            return await handler.Handle(request);
        }
    }
}
