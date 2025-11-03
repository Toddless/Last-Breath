namespace Core.Interfaces.Mediator
{
    using System.Threading.Tasks;

    public interface IMediator
    {
        Task<TResponce> Send<TRequest, TResponce>(TRequest request)
          where TRequest : IRequest<TResponce>;

        void Publish<TEvent>(TEvent evt)
          where TEvent : IEvent;
    }
}
