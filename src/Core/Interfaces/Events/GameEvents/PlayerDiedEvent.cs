namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerDiedEvent(IFightable Player) : IGameEvent, IBattleEvent;
}
