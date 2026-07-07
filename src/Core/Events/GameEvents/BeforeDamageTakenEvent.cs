namespace Core.Events.GameEvents
{
    using Battle;
    using Context;

    public record BeforeDamageTakenEvent(IDamageContext Context) : ICombatEvent, IBattleEvent;
}
