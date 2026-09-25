namespace Core.Events
{
    using Battle;

    public record AfterAttackEvent(IAttackContext Context) : ICombatEvent;

}
