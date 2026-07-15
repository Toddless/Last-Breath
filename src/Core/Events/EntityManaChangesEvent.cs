namespace Core.Events
{
    using Entity;

    public record EntityManaChangesEvent(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}
