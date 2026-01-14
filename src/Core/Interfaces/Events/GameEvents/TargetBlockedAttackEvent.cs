namespace Core.Interfaces.Events.GameEvents
{
    using Battle;

    public record TargetBlockedAttackEvent(IAttackContext Context) : IBattleEvent, ICombatEvent;
}
