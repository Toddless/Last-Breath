namespace Core.Entity
{
    using System.Collections.Generic;
    using Modifiers;

    /// <summary>
    /// Resolves an NPC modifier's NpcBuffId into parameter modifiers ("double health" etc.).
    /// Loot-only modifier kinds (tiers, rarities, guaranteed items) have no parameter side —
    /// their buff ids resolve to an empty list.
    /// </summary>
    public interface INpcBuffProvider
    {
        /// <summary>Fresh modifier instances for the buff, stamped with <paramref name="source"/>.</summary>
        IReadOnlyList<IModifierInstance> CreateModifiers(string buffId, string source);
    }
}
