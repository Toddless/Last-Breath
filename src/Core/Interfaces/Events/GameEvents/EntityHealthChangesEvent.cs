namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityHealthChangesEvent(IEntity Entity, float Value) : IGameEvent, IBattleEvent;
}
