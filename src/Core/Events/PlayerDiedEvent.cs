namespace Core.Events
{
    using Entity;

    public record PlayerDiedEvent(IFightable Player) : IGameEvent, IBattleEvent;
}
