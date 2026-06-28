namespace LootGeneration.Internal
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;

    internal class GameMessageBus(IGameServiceProvider serviceProvider) : IGameMessageBus
    {
        public async Task<TResponce> SendRequest<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>
        {
            var handler = serviceProvider.GetService<IRequestHandler<TRequest, TResponce>>();
            return await handler.HandleRequest(request);
        }

        public async Task PublishMessageAsync<TEvent>(TEvent message)
            where TEvent : IMessage
        {
            var handlers = serviceProvider.GetServices<IMessageHandler<TEvent>>();
            var tasks = handlers.Select(x => x.HandleMessageAsync(message));
            await Task.WhenAll(tasks);
        }
    }
}
