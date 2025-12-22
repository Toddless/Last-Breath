namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityMaxManaChangesGameEvent(IEntity Entity, float Value) : IBattleEvent;
}
