namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityBarrierChanges(IEntity Entity, float Value) : IGameEvent;
}
