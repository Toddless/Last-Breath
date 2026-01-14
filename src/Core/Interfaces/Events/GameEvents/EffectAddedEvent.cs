namespace Core.Interfaces.Events.GameEvents
{
    using Entity;
    using Abilities;

    public record EffectAddedEvent(IEffect Effect, IEntity Target) : IBattleEvent, IGameEvent;
}
