namespace Core.Events
{
    using Battle.Abilities;
    using Entity;

    public record EffectRemovedEvent(IEffect Effect, IFightable Target) : IBattleEvent, IGameEvent;
}
