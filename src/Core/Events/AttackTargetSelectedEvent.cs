namespace Core.Events
{
    using Entity;

    public record AttackTargetSelectedEvent(IFightable Target) : IBattleEvent;
}
