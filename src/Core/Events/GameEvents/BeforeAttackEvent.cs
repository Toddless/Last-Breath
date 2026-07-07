namespace Core.Events.GameEvents
{
    using Battle;

    public record BeforeAttackEvent(IAttackContext Context) : ICombatEvent
    {
    }
}
