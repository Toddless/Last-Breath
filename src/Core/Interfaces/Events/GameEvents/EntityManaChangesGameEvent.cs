namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityManaChangesGameEvent(IEntity Entity, float Value) : IGameEvent, IBattleEvent;
}
