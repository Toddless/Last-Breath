namespace Core.Interfaces.Mediator
{
    using System.Threading.Tasks;

    public interface IMediator
    {
        void Send<TRequest>(TRequest request)
            where TRequest : IRequest;
        Task<TResponce> Send<TRequest, TResponce>(TRequest request)
          where TRequest : IRequestWithResponce<TResponce>;
        void Publish<TEvent>(TEvent evt)
          where TEvent : IEvent;
    }
}
