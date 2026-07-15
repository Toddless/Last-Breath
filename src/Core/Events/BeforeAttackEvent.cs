namespace Core.Events
{
    using Battle;

    public record BeforeAttackEvent(IAttackContext Context) : ICombatEvent
    {
    }
}
