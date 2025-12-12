namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerManaChangesGameEvent(IEntity Player, float Value) : IGameEvent;
}
