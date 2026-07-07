namespace Core.Events.GameEvents
{
    using Entity;

    public record EntityManaChangesEvent(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}
