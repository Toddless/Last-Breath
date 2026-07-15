namespace Core.Ai
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Actions;
    using Considerations;
    using Entity;
    using Entity.Components;

    /// <summary>
    /// Utility-scored NPC turn: greedy cast loop with re-evaluation after every cast
    /// (a cast may kill, heal or unlock anything), then the closing basic attack.
    /// Simple intellect is clamped to one cast and skips considerations entirely.
    /// </summary>
    public class UtilityTurnPlanner(IRandomNumberGenerator rnd) : ICombatTurnPlanner
    {
        private readonly IReadOnlyList<ICombatConsideration> _considerations =
        [
            new FinisherConsideration(),
            new SelfPreservationConsideration(),
            new ManaBudgetConsideration(),
            new AlreadyAffectedConsideration(),
        ];

        public async Task PlayTurnAsync(IFightable self, IBehaviorProfile profile, ICombatEnvironment environment)
        {
            var board = new CombatBlackboard(self, profile, environment, rnd);

            board.Refresh();
            if (ShouldFlee(board))
            {
                await environment.FleeBattleAsync(self); // broken morale consumes the whole turn
                return;
            }

            int castBudget = profile.Intellect == AiIntellect.Simple ? 1 : profile.MaxCastsPerTurn;

            while (board.CastsThisTurn < castBudget && self.IsAlive)
            {
                board.Refresh();
                if (!board.HasLivingEnemies) return; // the fight is effectively over

                var best = PickBestCast(board);
                if (best == null) break;
                await best.ExecuteAsync(board);
            }

            if (!self.IsAlive) return; // e.g. a health-costed cast finished the caster off
            board.Refresh();

            var attack = new BasicAttackAction();
            if (attack.CanExecute(board))
                await attack.ExecuteAsync(board);
        }

        /// <summary>Fleeing only makes sense while enemies still stand — a won fight is no reason to run.</summary>
        private static bool ShouldFlee(CombatBlackboard board) =>
            board.Profile.FleeHealthThreshold > 0
            && board.HasLivingEnemies
            && CombatBlackboard.HealthRatio(board.Self) <= board.Profile.FleeHealthThreshold;

        private CastAbilityAction? PickBestCast(CombatBlackboard board)
        {
            CastAbilityAction? best = null;
            float bestScore = board.Profile.CastScoreThreshold;

            foreach (var action in BuildCastActions(board))
            {
                if (!action.CanExecute(board)) continue;
                float score = action.Score(board);
                if (score <= bestScore) continue;
                bestScore = score;
                best = action;
            }

            return best;
        }

        /// <summary>One candidate per equipped ability the profile knows about.</summary>
        private IEnumerable<CastAbilityAction> BuildCastActions(CombatBlackboard board) =>
            board.Self.AbilityBook.ActiveAbilities
                .Select(ability => (ability, behavior: board.Profile.GetBehaviorFor(ability.Id)))
                .Where(pair => pair.behavior != null)
                .Select(pair => new CastAbilityAction(pair.ability, pair.behavior!, _considerations));
    }
}
