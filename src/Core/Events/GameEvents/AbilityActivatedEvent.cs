namespace Core.Events.GameEvents
{
    using Battle;
    using Battle.Abilities;
    using Data;
    using Entity;

    /// <summary>
    /// <paramref name="Caster"/> and <paramref name="Vitals"/> are carried explicitly so replay
    /// consumers don't resolve live state. Vitals are captured after the cost is paid.
    /// <paramref name="CastId"/> ties this activation to the damage contexts it produced,
    /// letting the BattleDirector play the whole cast as one chord.
    /// </summary>
    public record AbilityActivatedEvent(IAbility Ability, IFightable Caster, VitalsSnapshot Vitals, string CastId) : IBattleEvent, ICombatEvent, IGameEvent;
}
