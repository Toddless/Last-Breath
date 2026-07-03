namespace Core.Interfaces.Events.GameEvents
{
    using Abilities;
    using Battle;
    using Data;
    using Entity;

    /// <summary>
    /// <paramref name="Caster"/> and <paramref name="Vitals"/> are carried explicitly so replay
    /// consumers don't resolve live state. Vitals are captured after the cost is paid.
    /// </summary>
    public record AbilityActivatedEvent(IAbility Ability, IFightable Caster, VitalsSnapshot Vitals) : IBattleEvent, ICombatEvent, IGameEvent;
}
