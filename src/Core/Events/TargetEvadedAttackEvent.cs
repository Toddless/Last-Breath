namespace Core.Events
{
    using Battle;

    public record TargetEvadedAttackEvent(IAttackContext Context): IBattleEvent, ICombatEvent;
}
