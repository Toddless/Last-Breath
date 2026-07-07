namespace Core.Events.GameEvents
{
    using Battle;
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// Published on the caster's combat bus when the ability finished executing (riders included).
    /// Closes the Activated→Executed window that cast-scoped mechanics live in — e.g. "the next
    /// activated ability ..." modifiers attach on Activated and detach here.
    /// </summary>
    public record AbilityExecutedEvent(IAbility Ability, IFightable Caster, string CastId) : ICombatEvent, IBattleEvent;
}
