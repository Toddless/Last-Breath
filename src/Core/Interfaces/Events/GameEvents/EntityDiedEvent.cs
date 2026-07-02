namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityDiedEvent(IFightable Entity) : IGameEvent, IBattleEvent;
}
