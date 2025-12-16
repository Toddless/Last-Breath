namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record EntityDiedGameEvent(IEntity Entity) : IGameEvent, IBattleEvent;
}
