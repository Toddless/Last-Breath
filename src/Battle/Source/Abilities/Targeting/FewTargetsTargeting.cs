namespace Battle.Source.Abilities.Targeting
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;

    /// <summary>Up to <paramref name="maxTargets"/> targets the player picks manually, then confirms.</summary>
    public sealed class FewTargetsTargeting(TargetRelation relation, int maxTargets) : ITargetingStrategy
    {
        public int MaxTargets => maxTargets;
        public bool RequiresManualSelection => true;

        /// <summary>Whose side the picks land on — read by the tooltip wording, decided here.</summary>
        public TargetRelation Relation => relation;

        public IReadOnlyList<IFightable> GetValidTargets(IFightable caster, IBattleField field) =>
            relation.Resolve(caster, field);
    }
}
