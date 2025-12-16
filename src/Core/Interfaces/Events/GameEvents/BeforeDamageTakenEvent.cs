namespace Core.Interfaces.Events.GameEvents
{
    using Battle;

    public record BeforeDamageTakenEvent(IAttackContext Context) : ICombatEvent, IBattleEvent;
}
