namespace Core.Entity.NpcModifiers
{
    using System.Collections.Generic;
    using Entity;

    /// <summary>Keeps the bearer's side of a modifier list in sync with the list itself: what a modifier's
    /// NpcBuffId promises (parameters, granted passives) is on the NPC exactly while the modifier is, and
    /// worth exactly what the scaling modifiers currently present make it worth.</summary>
    public interface INpcBuffBinder
    {
        /// <summary>Brings the bearer in line with <paramref name="modifiers"/>: binds what is new, rebinds
        /// what changed scale, unbinds what left. Idempotent — calling it twice changes nothing.</summary>
        void Rebuild(IFightable owner, IReadOnlyList<INpcModifier> modifiers);
    }
}
