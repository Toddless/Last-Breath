namespace Battle.Source.Conditions
{
    using System;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class PoisonStacksBossCapableExecuteCondition(Func<int> stackThreshold, float bossThresholdMultiplier = 1f) : IExecuteCondition
    {
        public bool ShouldExecute(IEntity target, IAttackContext? context = null)
        {
            int stacks = target.Effects.GetBy(e => e.Status == StatusEffects.Poison).Count();
            float threshold = stackThreshold();
            if (target is INpc { EntityType: EntityType.Boss or EntityType.Archon })
                threshold *= bossThresholdMultiplier;

            return stacks > threshold;
        }
    }
}
