namespace Core.Ai.World
{
    using System.Collections.Generic;

    public enum WorldActivityType : byte
    {
        Idle,
        Wander,
        Patrol
    }

    /// <summary>World behavior tuning of one NPC (the "world" section of Npc.json).</summary>
    public class WorldBrainConfig
    {
        public float VisionRadius { get; init; } = 350f;
        public float HearingRadius { get; init; } = 600f;

        /// <summary>Max distance from home the chase may reach before the NPC gives up.</summary>
        public float LeashRadius { get; init; } = 900f;

        public float MoveSpeed { get; init; } = 120f;
        public float ChaseSpeedMultiplier { get; init; } = 1.6f;

        /// <summary>How long the NPC inspects a noise point before calming down.</summary>
        public float SuspiciousSeconds { get; init; } = 4f;

        /// <summary>How long the NPC lingers around the last known target position.</summary>
        public float SearchSeconds { get; init; } = 5f;

        /// <summary>No re-aggression right after a battle — gives the player room to disengage.</summary>
        public float PostBattleGraceSeconds { get; init; } = 5f;

        /// <summary>False = never chases (city dwellers): investigates noises but does not attack.</summary>
        public bool Aggressive { get; init; } = true;

        /// <summary>
        /// Personal override: treats the player as an enemy regardless of the faction standing
        /// (bandits, beasts). Without it hostility comes from IFactionRelationService.
        /// </summary>
        public bool HostileToPlayer { get; init; }

        public WorldActivityType Activity { get; init; } = WorldActivityType.Idle;

        /// <summary>Wander destinations are rolled within this radius around home.</summary>
        public float WanderRadius { get; init; } = 250f;

        /// <summary>Pause at a reached wander/patrol point before picking the next one.</summary>
        public float ActivityPauseSeconds { get; init; } = 2f;

        /// <summary>
        /// Daily routine (the "schedule" section): time windows switching the Calm activity.
        /// Empty = the single <see cref="Activity"/> runs all day. Gaps fall back to it too.
        /// </summary>
        public IReadOnlyList<ScheduleSlotConfig> Schedule { get; init; } = [];
    }

    /// <summary>One schedule window in minutes of day (wrap through midnight allowed).</summary>
    public record ScheduleSlotConfig(int FromMinuteOfDay, int ToMinuteOfDay, WorldActivityType Activity, float? WanderRadius = null);
}
