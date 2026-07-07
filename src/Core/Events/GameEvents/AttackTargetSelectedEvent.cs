namespace Core.Events.GameEvents
{
    using Entity;

    public record AttackTargetSelectedEvent(IFightable Target) : IBattleEvent;
}
