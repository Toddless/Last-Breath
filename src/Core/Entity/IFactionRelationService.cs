namespace Core.Entity
{
    using System;
    using Enums;

    /// <summary>
    /// Who considers whom an enemy. Faction-vs-faction relations are DIRECTED and static
    /// (undead assault demons, demons stay neutral to everyone); the player's standing is
    /// per-faction and dynamic — quests and deeds move it along the RelationLevel ladder.
    /// </summary>
    public interface IFactionRelationService
    {
        /// <summary>How <paramref name="from"/> treats <paramref name="to"/>. Same faction = Alliance, unspecified = Neutral.</summary>
        RelationLevel GetRelation(Fractions from, Fractions to);

        /// <summary>True when <paramref name="from"/> attacks <paramref name="to"/> on sight (Hatred/Hostility).</summary>
        bool IsHostile(Fractions from, Fractions to);

        RelationLevel GetPlayerRelation(Fractions faction);
        void SetPlayerRelation(Fractions faction, RelationLevel level);

        /// <summary>True when this faction attacks the player on sight (Hatred/Hostility standing).</summary>
        bool IsHostileToPlayer(Fractions faction);

        event Action<Fractions, RelationLevel>? PlayerRelationChanged;
    }
}
