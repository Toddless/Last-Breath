namespace Battle.Source.Conditions
{
    using System;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class PoisonStackExecuteCondition(Func<int> stackThreshold) : IExecuteCondition
    {
        public bool ShouldExecute(IFightable target, IAttackContext? context = null)
        {
            if (target is not IFightableNpc npc) return false;
            int stacks = npc.Effects.GetBy(e => e.Status == StatusEffects.Poison).Count();
            bool isBossOrArchon = npc.EntityType is EntityType.Boss or EntityType.Archon;

            return stacks > stackThreshold() && !isBossOrArchon;
        }
    }
}
