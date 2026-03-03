namespace LastBreath.Services
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;

    public class GameMessageBus : IGameMessageBus
    {
        public async Task<TResponce> Send<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>
        {
            var handler = GameServiceProvider.Instance.GetService<IRequestHandler<TRequest, TResponce>>();
            return await handler.HandleRequest(request);
        }

        public async Task PublishAsync<TEvent>(TEvent evt)
            where TEvent : IEvent
        {
            var handlers = GameServiceProvider.Instance.GetServices<IEventHandler<TEvent>>();
            var tasks = handlers.Select(x => x.HandleAsync(evt));
            await Task.WhenAll(tasks);
        }
    }
}
