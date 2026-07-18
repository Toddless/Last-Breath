namespace Core.Events
{
    using Battle;
    using Battle.Abilities;
    using Entity;

    /// <summary>Published when a multicast (Intelligence stance) ability resolves its activation stage.
    /// Recorded by the timeline; the director republishes it at show time (battle log stage line).</summary>
    public record AbilityStageActivatedEvent(IAbility Ability, IFightable Caster, int Stage) : ICombatEvent, IBattleEvent;
}
