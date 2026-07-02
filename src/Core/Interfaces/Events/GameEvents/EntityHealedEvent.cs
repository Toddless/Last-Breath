namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;

    public record EntityHealedEvent(IFightable Healed, float Amount) : IBattleEvent, ICombatEvent;
}
