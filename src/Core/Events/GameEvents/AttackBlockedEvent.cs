namespace Core.Events.GameEvents
{
    using Battle;

    public record AttackBlockedEvent(IAttackContext Context) : ICombatEvent, IBattleEvent;
}
