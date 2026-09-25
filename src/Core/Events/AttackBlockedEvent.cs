namespace Core.Events
{
    using Battle;

    public record AttackBlockedEvent(IAttackContext Context) : ICombatEvent, IBattleEvent;
}
