namespace Core.Interfaces.Abilities
{
    using System.Collections.Generic;
    using Battle;
    using Entity;

    /// <summary>
    /// Decides how an ability picks its targets. Swappable per ability (single/few/self/random).
    /// The selection phase asks <see cref="GetValidTargets"/> for the highlightable set; manual
    /// strategies let the player click up to <see cref="MaxTargets"/>, automatic ones
    /// (<see cref="RequiresManualSelection"/> == false) resolve through <see cref="ResolveAutomatic"/>.
    /// </summary>
    public interface ITargetingStrategy
    {
        /// <summary>Upper bound on chosen targets. 1 for single-target modes.</summary>
        int MaxTargets { get; }

        /// <summary>False = no player clicks needed (self, random); the controller resolves and commits at once.</summary>
        bool RequiresManualSelection { get; }

        /// <summary>The set the player may pick from — used to highlight valid spots.</summary>
        IReadOnlyList<IFightable> GetValidTargets(IFightable caster, IBattleField field);

        /// <summary>Automatic resolution for non-manual strategies. Manual strategies never call it.</summary>
        IReadOnlyList<IFightable> ResolveAutomatic(IFightable caster, IBattleField field) => [];
    }
}
