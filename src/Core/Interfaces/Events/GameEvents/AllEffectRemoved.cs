namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record AllEffectRemoved(IEntity Target) : IBattleEvent, IGameEvent;
}
