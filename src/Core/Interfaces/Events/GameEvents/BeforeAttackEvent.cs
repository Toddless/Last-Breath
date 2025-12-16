namespace Core.Interfaces.Events.GameEvents
{
    using Battle;

    public record BeforeAttackEvent( IAttackContext Context) : ICombatEvent
    {
    }
}
