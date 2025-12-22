namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityMaxHealthChangesGameEvent(IEntity Entity, float Value) : IBattleEvent;
}
