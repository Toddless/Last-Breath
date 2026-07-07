namespace Battle.Source.Abilities.Targeting
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;

    /// <summary>One target the player clicks: <c>Enemy</c> or <c>Ally</c>. Commits on the click.</summary>
    public sealed class SingleTargetTargeting(TargetRelation relation) : ITargetingStrategy
    {
        public int MaxTargets => 1;
        public bool RequiresManualSelection => true;

        public IReadOnlyList<IFightable> GetValidTargets(IFightable caster, IBattleField field) =>
            relation.Resolve(caster, field);
    }
}
