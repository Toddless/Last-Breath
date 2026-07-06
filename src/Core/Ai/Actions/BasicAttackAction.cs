namespace Core.Ai.Actions
{
    using System.Threading.Tasks;
    using Interfaces.Entity;
    using Targeting;

    /// <summary>The turn-closing basic attack against the highest-value living enemy.</summary>
    public class BasicAttackAction : ICombatAction
    {
        private IFightable? _target;

        public bool CanExecute(CombatBlackboard board)
        {
            _target = TargetRanker.BestEnemy(board);
            return _target is { IsAlive: true };
        }

        /// <summary>Always worth taking once casting is done; the planner doesn't rank it against casts.</summary>
        public float Score(CombatBlackboard board) => 1f;

        public Task ExecuteAsync(CombatBlackboard board) =>
            _target == null ? Task.CompletedTask : board.Environment.BasicAttackAsync(board.Self, _target);
    }
}
