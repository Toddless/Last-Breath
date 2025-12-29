namespace Core.Interfaces.Events.GameEvents
{
    using Battle;

    public record AttackEvadedEvent(IAttackContext Context) : ICombatEvent, IBattleEvent;
}
