namespace Core.Ai.Considerations
{
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Entity;

    /// <summary>Damage casts get more attractive the closer the best target is to death (kill-secure).</summary>
    public class FinisherConsideration(float pressure = 0.75f) : ICombatConsideration
    {
        public float Evaluate(CombatBlackboard board, IAbility ability, AbilityRole role, IReadOnlyList<IFightable> targets)
        {
            if (role != AbilityRole.Damage || targets.Count == 0) return 1f;

            float lowestRatio = targets.Min(CombatBlackboard.HealthRatio);
            return 1f + (1f - lowestRatio) * pressure;
        }
    }
}
