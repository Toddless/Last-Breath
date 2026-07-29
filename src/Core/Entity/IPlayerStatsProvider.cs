namespace Core.Entity
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// Source of the player's base parameter values; the data side loads them from the PlayerStats
    /// catalog. Named profiles are a detail of the file format, so a second baseline is a JSON edit
    /// rather than a second reader.
    /// </summary>
    public interface IPlayerStatsProvider
    {
        /// <summary>The unarmed baseline: what the player is worth with no weapon and no gear.</summary>
        IReadOnlyDictionary<EntityParameter, float> Unarmed { get; }

        /// <summary>Base value from the unarmed profile. A parameter the profile does not mention answers
        /// zero, exactly as the hand-written switch it replaced did.</summary>
        float UnarmedValue(EntityParameter parameter);
    }
}
