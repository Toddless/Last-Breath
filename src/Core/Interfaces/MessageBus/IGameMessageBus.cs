namespace Core.Interfaces.MessageBus
{
    using Events;
    using System.Threading.Tasks;

    public interface IGameMessageBus
    {
        Task<TResponce> SendRequest<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>;

        Task PublishMessageAsync<TMessage>(TMessage message)
            where TMessage : IMessage;
    }
}
