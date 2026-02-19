namespace Core.Interfaces.MessageBus
{
    using Events;
    using System.Threading.Tasks;

    public interface IGameMessageBus
    {
        Task<TResponce> Send<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>;

        Task PublishAsync<TEvent>(TEvent evt)
            where TEvent : IEvent;
    }
}
