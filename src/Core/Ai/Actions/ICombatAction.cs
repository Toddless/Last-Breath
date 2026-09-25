namespace Core.Ai.Actions
{
    using System.Threading.Tasks;

    /// <summary>
    /// One executable choice of an NPC turn. Actions are rebuilt every planner iteration
    /// (the world changes after each cast), so instances may cache their scoring decisions.
    /// </summary>
    public interface ICombatAction
    {
        bool CanExecute(CombatBlackboard board);
        float Score(CombatBlackboard board);
        Task ExecuteAsync(CombatBlackboard board);
    }
}
