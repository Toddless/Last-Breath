namespace Core.Reputation
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// The second reputation layer: what a CONCRETE NPC thinks of the player, keyed by InstanceId.
    /// Personal points shift the faction standing by whole relation-ladder steps in that NPC's
    /// eyes — a rescued dwarf won't attack even when the dwarves as a faction would. The memory
    /// dies with the person: final death and rising as undead erase it.
    /// </summary>
    public interface IPersonalReputationService
    {
        /// <summary>Save capture; live view, do not mutate.</summary>
        IReadOnlyDictionary<string, int> Snapshot { get; }

        int GetPersonal(string instanceId);

        void AddPersonal(string instanceId, int delta, string reason);

        /// <summary>Save-load path: direct write, clamped.</summary>
        void SetPersonal(string instanceId, int points);

        /// <summary>How many relation-ladder steps this NPC's opinion shifts the faction standing.</summary>
        int GetLevelShift(string instanceId);

        /// <summary>The faction standing shifted by this NPC's personal opinion, clamped to the ladder.</summary>
        RelationLevel GetEffectiveRelation(string instanceId, Fractions faction);

        /// <summary>True when THIS NPC attacks the player on sight (effective relation ≤ Hostility).</summary>
        bool IsHostileToPlayer(string instanceId, Fractions faction);

        void Forget(string instanceId);
    }
}
