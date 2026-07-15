namespace Core.Events
{
    using Battle;
    using Context;

    public record BeforeDamageTakenEvent(IDamageContext Context) : ICombatEvent, IBattleEvent;
}
