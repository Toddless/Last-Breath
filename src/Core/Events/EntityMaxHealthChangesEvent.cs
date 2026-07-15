namespace Core.Events
{
    using Entity;

    public record EntityMaxHealthChangesEvent(IFightable Entity, float Value) : IBattleEvent;
}
