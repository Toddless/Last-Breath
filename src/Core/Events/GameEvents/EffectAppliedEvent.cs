namespace Core.Events.GameEvents
{
    using Battle;
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// An effect instance was ACCEPTED by the target's stacking rules (rejected stacks don't fire).
    /// Recorded in the timeline at resolve; the director republishes it at show time — the battle log
    /// and future application VFX react in sync with the presentation.
    /// </summary>
    public record EffectAppliedEvent(IEffect Effect, IFightable Target, IFightable Caster) : ICombatEvent, IBattleEvent;
}
