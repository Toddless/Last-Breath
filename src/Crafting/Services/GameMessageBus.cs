namespace Crafting.Services
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;

    internal class GameMessageBus : IGameMessageBus
    {
        public async Task<TResponce> SendRequest<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>
        {
            var handler = GameServiceProvider.Instance.GetService<IRequestHandler<TRequest, TResponce>>();
            return await handler.HandleRequest(request);
        }

        public async Task PublishMessageAsync<TEvent>(TEvent message)
            where TEvent : IMessage
        {
            var handlers = GameServiceProvider.Instance.GetServices<IMessageHandler<TEvent>>();
            var tasks = handlers.Select(x => x.HandleMessageAsync(message));
            await Task.WhenAll(tasks);
        }
    }
}
