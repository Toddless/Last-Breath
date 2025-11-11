namespace Core.Interfaces.Events
{
    using System.Threading.Tasks;

    public interface IEventHandler<TEvent>
        where TEvent : IEvent
    {
        Task HandleAsync(TEvent evnt);
    }
}
