namespace Core.Interfaces.Mediator
{
    using System;
    using Events;
    using System.Threading.Tasks;

    public interface IMediator
    {
        event Action? UpdateUi;

        Task<TResponce> Send<TRequest, TResponce>(TRequest request)
            where TRequest : IRequest<TResponce>;

        Task PublishAsync<TEvent>(TEvent evt)
            where TEvent : IEvent;

        void RaiseUpdateUi();
    }
}
