namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityMaxHealthChangesEvent(IEntity Entity, float Value) : IBattleEvent;
}
