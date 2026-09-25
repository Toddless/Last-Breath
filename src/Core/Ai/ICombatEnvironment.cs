namespace Core.Ai
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle;
    using Battle.Abilities;
    using Entity;

    /// <summary>
    /// Battle primitives the planner acts through. Implemented by the arena: casting goes
    /// straight to Ability.Execute (the TargetSelectionController path is player UI only),
    /// the basic attack reuses the arena's attack context scheduling and ends the turn.
    /// </summary>
    public interface ICombatEnvironment
    {
        IBattleField Field { get; }
        Task CastAbilityAsync(IFightable caster, IAbility ability, IReadOnlyList<IFightable> targets);
        Task BasicAttackAsync(IFightable attacker, IFightable target);

        /// <summary>The fighter leaves the battle alive (broken morale). Consumes its turn.</summary>
        Task FleeBattleAsync(IFightable fighter);
    }
}
