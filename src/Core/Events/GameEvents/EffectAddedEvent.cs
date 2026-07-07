namespace Core.Events.GameEvents
{
    using Battle.Abilities;
    using Entity;

    public record EffectAddedEvent(IEffect Effect, IFightable Target) : IBattleEvent, IGameEvent;
}
