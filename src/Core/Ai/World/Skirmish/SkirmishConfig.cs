namespace Core.Ai.World.Skirmish
{
    /// <summary>Tuning of the abstract NPC-vs-NPC combat (design defaults; tests shorten the intervals).</summary>
    public class SkirmishConfig
    {
        public int Rounds { get; init; } = 3;

        /// <summary>Each d20 roll happens after a random pause in this window.</summary>
        public float MinRollIntervalSeconds { get; init; } = 30f;
        public float MaxRollIntervalSeconds { get; init; } = 120f;

        /// <summary>Chance that non-undead winners burn each undead loser's body (deliberately not 100%).</summary>
        public float UndeadBurnChance { get; init; } = 0.6f;

        /// <summary>
        /// Presentation (future): when the player is nearby, the round winner's attack and the
        /// loser's hurt animations play with random pauses in this window. Not implemented yet —
        /// the NpcSkirmishRoundResolvedEvent is the hook.
        /// </summary>
        public float PresentationPauseMinSeconds { get; init; } = 5f;
        public float PresentationPauseMaxSeconds { get; init; } = 10f;
    }
}
