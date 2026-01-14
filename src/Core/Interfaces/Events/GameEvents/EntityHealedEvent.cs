namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;

    public record EntityHealedEvent(IEntity Healed, float Amount) : IBattleEvent, ICombatEvent;
}
