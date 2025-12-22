namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityHealthChangesGameEvent(IEntity Entity, float Value) : IGameEvent, IBattleEvent;
}
