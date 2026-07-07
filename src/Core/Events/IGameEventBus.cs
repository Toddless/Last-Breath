namespace Core.Events
{
    using Interfaces;

    public interface IGameEventBus : IEventBus<IGameEvent>
    {
    }
}
