namespace Core.Interfaces.Events.GameEvents
{
    using Battle;

    public record AttackBlockedEvent(IAttackContext Context) : ICombatEvent
    {
    }
}
