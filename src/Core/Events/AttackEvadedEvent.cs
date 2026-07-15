namespace Core.Events
{
    using Battle;

    public record AttackEvadedEvent(IAttackContext Context) : ICombatEvent, IBattleEvent;
}
