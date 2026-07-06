namespace Core.Ai.Considerations
{
    using System.Collections.Generic;
    using Interfaces.Abilities;
    using Interfaces.Entity;

    /// <summary>
    /// Heals score with missing health: near-useless at full health, urgent when low.
    /// Other roles are untouched — buffs are fine to open a turn with at full health.
    /// </summary>
    public class SelfPreservationConsideration : ICombatConsideration
    {
        private const float FullHealthFloor = 0.25f;
        private const float UrgencyScale = 1.5f;

        public float Evaluate(CombatBlackboard board, IAbility ability, AbilityRole role, IReadOnlyList<IFightable> targets)
        {
            if (role != AbilityRole.Heal) return 1f;

            float missing = 1f - CombatBlackboard.HealthRatio(board.Self);
            return FullHealthFloor + missing * UrgencyScale;
        }
    }
}
