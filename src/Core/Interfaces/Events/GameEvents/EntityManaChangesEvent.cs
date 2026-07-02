namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityManaChangesEvent(IFightable Entity, float Value) : IGameEvent, IBattleEvent;
}
