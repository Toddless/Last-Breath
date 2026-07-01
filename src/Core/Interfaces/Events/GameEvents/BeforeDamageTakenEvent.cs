namespace Core.Interfaces.Events.GameEvents
{
    using Battle;

    public record BeforeDamageTakenEvent(IDamageContext Context) : ICombatEvent, IBattleEvent;
}
