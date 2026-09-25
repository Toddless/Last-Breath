namespace Core.Ai.Considerations
{
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// Re-applying a buff/debuff/control to targets already under THIS ability's effects is
    /// mostly wasted — dampen it hard. Detection relies on the convention that effects carry
    /// the casting ability's InstanceId as their Source. Damage casts pass through.
    /// </summary>
    public class AlreadyAffectedConsideration : ICombatConsideration
    {
        private const float AllAffectedFactor = 0.2f;

        public float Evaluate(CombatBlackboard board, IAbility ability, AbilityRole role, IReadOnlyList<IFightable> targets)
        {
            if (role is not (AbilityRole.Buff or AbilityRole.Debuff or AbilityRole.Control)) return 1f;

            // Self-buffs resolve automatically at execution — judge by the caster itself then.
            IReadOnlyList<IFightable> affected = targets.Count > 0 ? targets : [board.Self];
            return affected.All(target => target.Effects?.GetBySource(ability.InstanceId).Any() == true)
                ? AllAffectedFactor
                : 1f;
        }
    }
}
