namespace Core.Interfaces.Events.GameEvents
{
    using Battle;

    public record TargetEvadedAttackEvent(IAttackContext Context): IBattleEvent, ICombatEvent;
}
