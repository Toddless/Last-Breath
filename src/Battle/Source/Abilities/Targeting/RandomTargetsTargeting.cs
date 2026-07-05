namespace Battle.Source.Abilities.Targeting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    /// <summary>Picks up to <paramref name="maxTargets"/> random targets — no player choice. Assigned in the
    /// ability factory to abilities that target randomly.</summary>
    public sealed class RandomTargetsTargeting(TargetRelation relation, int maxTargets) : ITargetingStrategy
    {
        public int MaxTargets => maxTargets;
        public bool RequiresManualSelection => false;

        public IReadOnlyList<IFightable> GetValidTargets(IFightable caster, IBattleField field) =>
            relation.Resolve(caster, field);

        public IReadOnlyList<IFightable> ResolveAutomatic(IFightable caster, IBattleField field)
        {
            var pool = relation.Resolve(caster, field).ToList();
            if (pool.Count <= maxTargets) return pool;

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return pool.Take(maxTargets).ToList();
        }
    }
}
