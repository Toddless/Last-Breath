namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerBarrierChangesGameEvent(IEntity Player, float Value) : IGameEvent;
}
