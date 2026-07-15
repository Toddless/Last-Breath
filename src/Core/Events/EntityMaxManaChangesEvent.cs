namespace Core.Events
{
    using Entity;

    public record EntityMaxManaChangesEvent(IFightable Entity, float Value) : IBattleEvent;
}
