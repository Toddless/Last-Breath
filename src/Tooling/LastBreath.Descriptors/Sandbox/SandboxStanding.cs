namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
    using Core.Entity;
    using Core.Enums;
    using Core.Reputation;

    /// <summary>
    /// What the world thinks of the player, as a dry run states it: one standing for every faction and
    /// one opinion for the person across the table. The points behind a standing are not modelled —
    /// every narrative entry asks after the LEVEL, and a level typed by its author is the question.
    /// </summary>
    /// <remarks>The events never fire: nothing in a dry run moves a standing on its own, and the panel
    /// redraws from the state it wrote rather than from a notification.</remarks>
    public sealed class SandboxStanding(SandboxWorldState state) : IFactionRelationService, IPersonalReputationService
    {
        public IReadOnlyDictionary<string, int> Snapshot { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        public event Action<ReputationChangedArgs>? PlayerReputationChanged { add { } remove { } }

        public event Action<Fractions, RelationLevel>? PlayerRelationChanged { add { } remove { } }

        /// <summary>Same faction is always allied; a dry run states nothing about faction-vs-faction
        /// matrices, which no narrative entry reads.</summary>
        public RelationLevel GetRelation(Fractions from, Fractions to) =>
            from == to ? RelationLevel.Alliance : RelationLevel.Neutral;

        public bool IsHostile(Fractions from, Fractions to) => GetRelation(from, to) <= RelationLevel.Hostility;

        public int GetReputation(Fractions faction) => 0;

        public void AddReputation(Fractions faction, int delta, string reason)
        {
        }

        public void SetReputation(Fractions faction, int points, RelationLevel? level = null)
        {
        }

        public void SetPlayerRelation(Fractions faction, RelationLevel level) => state.FactionStanding = level;

        public RelationLevel GetPlayerRelation(Fractions faction) => state.FactionStanding;

        public bool IsHostileToPlayer(Fractions faction) => state.FactionStanding <= RelationLevel.Hostility;

        public bool HasReputation(Fractions faction) => true;

        public bool CanRaid(Fractions faction) => false;

        public int GetPersonal(string instanceId) => 0;

        public void AddPersonal(string instanceId, int delta, string reason)
        {
        }

        public void SetPersonal(string instanceId, int points)
        {
        }

        public int GetLevelShift(string instanceId) => 0;

        public RelationLevel GetEffectiveRelation(string instanceId, Fractions faction) => state.NpcRelation;

        public bool IsHostileToPlayer(string instanceId, Fractions faction) => state.NpcRelation <= RelationLevel.Hostility;

        public void Forget(string instanceId)
        {
        }
    }
}
