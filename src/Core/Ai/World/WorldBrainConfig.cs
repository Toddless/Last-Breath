namespace Core.Ai.World
{
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

        public WorldActivityType Activity { get; init; } = WorldActivityType.Idle;

        /// <summary>Wander destinations are rolled within this radius around home.</summary>
        public float WanderRadius { get; init; } = 250f;

        /// <summary>Pause at a reached wander/patrol point before picking the next one.</summary>
        public float ActivityPauseSeconds { get; init; } = 2f;
    }
}
