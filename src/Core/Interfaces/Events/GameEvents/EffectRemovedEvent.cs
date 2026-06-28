namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Entity;

    public record EffectRemovedEvent(IEffect Effect, IEntity Target) : IBattleEvent, IGameEvent;
}
