namespace Core.Ai.Considerations
{
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// Heals score with the missing health of the most wounded planned target (self or an ally):
    /// near-useless on the healthy, urgent on the dying. Falls back to the caster when the
    /// targeting resolves automatically at execution. Other roles are untouched.
    /// </summary>
    public class SelfPreservationConsideration : ICombatConsideration
    {
        private const float FullHealthFloor = 0.25f;
        private const float UrgencyScale = 1.5f;

        public float Evaluate(CombatBlackboard board, IAbility ability, AbilityRole role, IReadOnlyList<IFightable> targets)
        {
            if (role != AbilityRole.Heal) return 1f;

            float lowestRatio = targets.Count > 0
                ? targets.Min(CombatBlackboard.HealthRatio)
                : CombatBlackboard.HealthRatio(board.Self);
            return FullHealthFloor + (1f - lowestRatio) * UrgencyScale;
        }
    }
}
