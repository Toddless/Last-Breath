namespace Core.Services
{
    using System.Linq;
    using System.Threading.Tasks;
    using Data;
    using Interfaces.Events;
    using Interfaces.MessageBus;

    /// <summary>Resolves handlers through the injected provider — no static service locator.</summary>
    public class GameMessageBus(IGameServiceProvider serviceProvider) : IGameMessageBus
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
            var tasks = handlers.Select(handler => handler.HandleMessageAsync(message));
            await Task.WhenAll(tasks);
        }
    }
}
