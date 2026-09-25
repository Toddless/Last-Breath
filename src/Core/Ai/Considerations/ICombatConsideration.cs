namespace Core.Ai.Considerations
{
    using System.Collections.Generic;
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// One multiplicative factor of a cast's utility. 1 = neutral, below dampens, above boosts.
    /// Considerations must stay side-effect free — they run on every planner iteration.
    /// </summary>
    public interface ICombatConsideration
    {
        float Evaluate(CombatBlackboard board, IAbility ability, AbilityRole role, IReadOnlyList<IFightable> targets);
    }
}
