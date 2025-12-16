namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityManaChanges(IEntity Entity, float Value) : IGameEvent, IBattleEvent;
}
