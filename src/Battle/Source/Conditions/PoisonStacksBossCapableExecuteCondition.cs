namespace Battle.Source.Conditions
{
    using System;
    using System.Linq;
    using Core.Battle;
    using Core.Entity;
    using Core.Enums;

    public class PoisonStacksBossCapableExecuteCondition(Func<int> stackThreshold, float bossThresholdMultiplier = 1f) : IExecuteCondition
    {
        public bool ShouldExecute(IFightable target, IAttackContext? context = null)
        {
            int stacks = target.Effects.GetBy(e => e.Status == StatusEffects.Poison).Count();
            float threshold = stackThreshold();
            if (target is IFightableNpc { EntityType: EntityType.Boss or EntityType.Archon })
                threshold *= bossThresholdMultiplier;

            return stacks > threshold;
        }
    }
}
