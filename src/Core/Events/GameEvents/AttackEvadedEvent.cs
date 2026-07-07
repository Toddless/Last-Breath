namespace Core.Events.GameEvents
{
    using Battle;

    public record AttackEvadedEvent(IAttackContext Context) : ICombatEvent, IBattleEvent;
}
