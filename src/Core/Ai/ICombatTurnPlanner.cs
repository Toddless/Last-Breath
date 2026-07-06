namespace Core.Ai
{
    using System.Threading.Tasks;
    using Interfaces.Entity;

    /// <summary>
    /// Plays one full NPC turn: zero or more free ability casts, then the closing basic attack.
    /// Pure logic — resolves instantly; the replay layer presents it afterwards as usual.
    /// </summary>
    public interface ICombatTurnPlanner
    {
        Task PlayTurnAsync(IFightable self, BehaviorProfile profile, ICombatEnvironment environment);
    }
}
