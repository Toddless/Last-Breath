namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityHealthChanges(IEntity Entity, float Value) : IGameEvent;
}
