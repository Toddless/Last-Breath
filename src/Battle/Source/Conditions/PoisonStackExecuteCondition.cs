namespace Battle.Source.Conditions
{
    using System;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class PoisonStackExecuteCondition(Func<int> stackThreshold) : IExecuteCondition
    {
        public bool ShouldExecute(IEntity target, IAttackContext? context = null)
        {
            if (target is not INpc npc) return false;
            int stacks = npc.Effects.GetBy(e => e.Status == StatusEffects.Poison).Count();
            bool isBossOrArchon = npc.EntityType is EntityType.Boss or EntityType.Archon;

            return stacks > stackThreshold() && !isBossOrArchon;
        }
    }
}
