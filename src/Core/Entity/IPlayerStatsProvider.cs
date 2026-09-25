namespace Core.Entity
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// Source of the player's base parameter values; the data side loads them from the PlayerStats
    /// catalog. One baseline is asked for and one is written down: a second would need a reader that
    /// knows when to prefer it.
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
