namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerDiedGameEvent(IEntity Player) : IGameEvent;
}
