namespace Core.Interfaces.Events.GameEvents
{
    using Entity;
    using Abilities;

    public record EffectRemovedEvent(IEffect Effect, IEntity Target) : IBattleEvent, IGameEvent;
}
