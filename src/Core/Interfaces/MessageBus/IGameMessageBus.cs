namespace Core.Interfaces.MessageBus
{
    using System.Threading.Tasks;
    using Events;

    public interface IGameMessageBus
    {
        Task<TResponce> SendRequest<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>;

        Task PublishMessageAsync<TMessage>(TMessage message)
            where TMessage : IMessage;
    }
}
