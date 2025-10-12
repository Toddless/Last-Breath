namespace Core.Interfaces.Mediator
{

    public interface IMediator
    {
        void Send<TRequest>(TRequest request)
            where TRequest : IRequest;
        void Publish<TEvent>(TEvent evt)
          where TEvent : IEvent;
    }
}
