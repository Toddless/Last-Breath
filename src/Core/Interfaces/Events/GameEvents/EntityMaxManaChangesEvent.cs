namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityMaxManaChangesEvent(IEntity Entity, float Value) : IBattleEvent;
}
