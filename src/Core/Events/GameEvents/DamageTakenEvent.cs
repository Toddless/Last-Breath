namespace Core.Events.GameEvents
{
    using Battle;
    using Context;
    using Data;
    using Entity;

    /// <summary><paramref name="Vitals"/> is the target's state right after the hit was applied.</summary>
    public record DamageTakenEvent(IDamageContext Context, IFightable Target, VitalsSnapshot Vitals) : ICombatEvent, IBattleEvent;
}
