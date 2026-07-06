namespace Core.Ai.Targeting
{
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces.Entity;

    /// <summary>
    /// Orders a valid-target set by desirability for the given role.
    /// Offensive roles favor low health (kill-secure) and high threat; supportive roles favor missing health.
    /// </summary>
    public static class TargetRanker
    {
        private const float MissingHealthWeight = 1f;
        private const float ThreatWeight = 0.5f;

        public static IReadOnlyList<IFightable> Rank(IReadOnlyList<IFightable> candidates, AbilityRole role)
        {
            if (candidates.Count <= 1) return candidates;

            return IsOffensive(role)
                ? candidates.OrderByDescending(target => ScoreEnemy(target, candidates)).ToList()
                : candidates.OrderBy(CombatBlackboard.HealthRatio).ToList();
        }

        public static IFightable? BestEnemy(CombatBlackboard board) =>
            board.Enemies.Count == 0 ? null : board.Enemies.MaxBy(enemy => ScoreEnemy(enemy, board.Enemies));

        private static bool IsOffensive(AbilityRole role) => role is AbilityRole.Damage or AbilityRole.Debuff or AbilityRole.Control;

        private static float ScoreEnemy(IFightable enemy, IReadOnlyList<IFightable> all)
        {
            float maxDamage = all.Max(candidate => candidate.Parameters.Damage);
            float threat = maxDamage <= 0 ? 0f : enemy.Parameters.Damage / maxDamage;
            return (1f - CombatBlackboard.HealthRatio(enemy)) * MissingHealthWeight + threat * ThreatWeight;
        }
    }
}
