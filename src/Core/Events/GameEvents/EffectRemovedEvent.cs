namespace Core.Events.GameEvents
{
    using Battle.Abilities;
    using Entity;

    public record EffectRemovedEvent(IEffect Effect, IFightable Target) : IBattleEvent, IGameEvent;
}
