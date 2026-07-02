namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;
    using Enums;

    public record DamageTakenEvent(IDamageContext Context, IFightable Target) : ICombatEvent, IBattleEvent;
}
