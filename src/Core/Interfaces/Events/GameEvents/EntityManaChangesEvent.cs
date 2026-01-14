namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityManaChangesEvent(IEntity Entity, float Value) : IGameEvent, IBattleEvent;
}
