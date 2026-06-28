namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Entity;

    public record EffectAddedEvent(IEffect Effect, IEntity Target) : IBattleEvent, IGameEvent;
}
