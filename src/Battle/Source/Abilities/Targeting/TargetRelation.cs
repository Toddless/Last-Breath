namespace Battle.Source.Abilities.Targeting
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Entity;

    /// <summary>Which side of the field a targeting strategy pulls its candidates from.</summary>
    public enum TargetRelation
    {
        Enemies,
        Allies
    }

    public static class TargetRelationExtensions
    {
        public static IReadOnlyList<IFightable> Resolve(this TargetRelation relation, IFightable caster, IBattleField field) =>
            relation switch
            {
                TargetRelation.Enemies => field.GetEnemies(caster),
                TargetRelation.Allies => field.GetAllies(caster),
                _ => []
            };
    }
}
