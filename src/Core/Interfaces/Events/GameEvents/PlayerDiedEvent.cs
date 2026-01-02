namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerDiedEvent(IEntity Player) : IGameEvent, IBattleEvent;
}
