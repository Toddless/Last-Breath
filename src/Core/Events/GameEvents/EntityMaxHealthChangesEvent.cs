namespace Core.Events.GameEvents
{
    using Entity;

    public record EntityMaxHealthChangesEvent(IFightable Entity, float Value) : IBattleEvent;
}
