namespace Battle.Source.Abilities.Targeting
{
    using System.Collections.Generic;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    /// <summary>Up to <paramref name="maxTargets"/> targets the player picks manually, then confirms.</summary>
    public sealed class FewTargetsTargeting(TargetRelation relation, int maxTargets) : ITargetingStrategy
    {
        public int MaxTargets => maxTargets;
        public bool RequiresManualSelection => true;

        public IReadOnlyList<IFightable> GetValidTargets(IFightable caster, IBattleField field) =>
            relation.Resolve(caster, field);
    }
}
