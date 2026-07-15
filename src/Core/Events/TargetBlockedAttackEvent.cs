namespace Core.Events
{
    using Battle;

    public record TargetBlockedAttackEvent(IAttackContext Context) : IBattleEvent, ICombatEvent;
}
