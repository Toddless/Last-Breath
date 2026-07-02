namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityMaxManaChangesEvent(IFightable Entity, float Value) : IBattleEvent;
}
