namespace Core.Events.GameEvents
{
    using Battle;
    using Data;
    using Entity;

    /// <summary><paramref name="Vitals"/> is the entity's state right after the heal was applied.</summary>
    public record EntityHealedEvent(IFightable Healed, float Amount, VitalsSnapshot Vitals) : IBattleEvent, ICombatEvent;
}
