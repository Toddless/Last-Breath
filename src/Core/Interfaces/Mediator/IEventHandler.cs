namespace Core.Interfaces.Mediator
{
    public interface IEventHandler<TEvent>
        where TEvent : IEvent
    {
        void Handle(TEvent evnt);
    }
}
