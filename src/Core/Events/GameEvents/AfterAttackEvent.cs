namespace Core.Events.GameEvents
{
    using Battle;

    public record AfterAttackEvent(IAttackContext Context) : ICombatEvent;

}
