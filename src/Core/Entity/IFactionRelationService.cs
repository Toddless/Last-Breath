namespace Core.Entity
{
    using System;
    using Enums;

    /// <summary>A reputation points change; the level may or may not have crossed a threshold with it.</summary>
    public record ReputationChangedArgs(Fractions Faction, int Points, int Delta, string Reason);

    /// <summary>
    /// Who considers whom an enemy. Faction-vs-faction relations are DIRECTED and static
    /// (undead assault demons, demons stay neutral to everyone). The player's standing is
    /// per-faction integer points: deeds move the points, the RelationLevel is derived from
    /// JSON thresholds with a hysteresis buffer so it doesn't flicker around a boundary.
    /// </summary>
    public interface IFactionRelationService
    {
        /// <summary>How <paramref name="from"/> treats <paramref name="to"/>. Same faction = Alliance, unspecified = Neutral.</summary>
        RelationLevel GetRelation(Fractions from, Fractions to);

        /// <summary>True when <paramref name="from"/> attacks <paramref name="to"/> on sight (Hatred/Hostility).</summary>
        bool IsHostile(Fractions from, Fractions to);

        int GetReputation(Fractions faction);

        /// <summary>Shifts the player's standing, clamped to the configured range. No-op for factions without reputation.</summary>
        void AddReputation(Fractions faction, int delta, string reason);

        /// <summary>Direct write (save-load, debug): the level is resolved from the thresholds unless a stored hysteresis state is supplied.</summary>
        void SetReputation(Fractions faction, int points, RelationLevel? level = null);

        /// <summary>Quest/design override: jumps the standing to the midpoint of the level's points band.</summary>
        void SetPlayerRelation(Fractions faction, RelationLevel level);

        RelationLevel GetPlayerRelation(Fractions faction);

        /// <summary>True when this faction attacks the player on sight (Hatred/Hostility standing).</summary>
        bool IsHostileToPlayer(Fractions faction);

        /// <summary>False for factions whose standing never moves (animals) — deeds are ignored.</summary>
        bool HasReputation(Fractions faction);

        /// <summary>True when the faction raids the player at Hatred standing.</summary>
        bool CanRaid(Fractions faction);

        /// <summary>Every points change, including ones that don't cross a level threshold.</summary>
        event Action<ReputationChangedArgs>? PlayerReputationChanged;

        /// <summary>Level threshold crossings only.</summary>
        event Action<Fractions, RelationLevel>? PlayerRelationChanged;
    }
}
