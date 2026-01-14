namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityDiedEvent(IEntity Entity) : IGameEvent, IBattleEvent;
}
