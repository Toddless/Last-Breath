namespace Core.Events
{
    using Battle;
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// The target's incoming-effect pipeline rejected the effect outright (boss control resistance).
    /// Recorded at resolve; the director's future "Immune!" beat and the battle log react at show time.
    /// </summary>
    public record EffectResistedEvent(IEffect Effect, IFightable Target, IFightable Caster) : ICombatEvent, IBattleEvent;
}
