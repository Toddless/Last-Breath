namespace Core.Entity
{
    using System.Collections.Generic;
    using Data.NpcBuffsData;
    using Modifiers;

    /// <summary>
    /// Resolves an NPC modifier's NpcBuffId into what the bearer actually gains: parameter modifiers
    /// ("double health") and grants (a passive skill the modifier hands over). A modifier kind with no
    /// bearer side still declares a buff id — its entry simply carries no lines.
    /// </summary>
    public interface INpcBuffProvider
    {
        /// <summary>Fresh modifier instances for the buff, stamped with <paramref name="source"/>.</summary>
        IReadOnlyList<IModifierInstance> CreateModifiers(string buffId, string source);

        /// <summary>Grant lines of the buff (passive skills); empty for a buff that grants nothing.</summary>
        IReadOnlyList<NpcBuffGrantData> GetGrants(string buffId);

        /// <summary>Whether the catalog holds an entry for the id at all. A non-empty NpcBuffId with no
        /// entry behind it is a broken link, not an empty buff — the two must never read the same.</summary>
        bool HasBuff(string buffId);
    }
}
