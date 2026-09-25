namespace Core.Events
{
    using Battle.Abilities;
    using Entity;

    public record EffectAddedEvent(IEffect Effect, IFightable Target) : IBattleEvent, IGameEvent;
}
