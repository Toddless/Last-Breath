namespace Core.Ai.World.Recovery
{
    /// <summary>
    /// Rest tuning (the Recovery catalog). Rates are shares of the maximum restored per GAME
    /// minute inside a recovery zone (campfire, spawn point); defaults are made-up placeholders.
    /// </summary>
    public class RecoveryConfig
    {
        public float HealthPercentPerMinute { get; init; } = 0.1f;
        public float ManaPercentPerMinute { get; init; } = 0.15f;
        public float BarrierPercentPerMinute { get; init; } = 0.15f;

        /// <summary>Zone radius used when the registering node does not supply its own (campfire).</summary>
        public float DefaultZoneRadius { get; init; } = 150f;

        /// <summary>NPC abandons its routine and heads home to rest below this health share.</summary>
        public float NpcRetreatHealthPercent { get; init; } = 0.5f;

        /// <summary>Resting at home without any health gain for this many game minutes = give up
        /// (no recovery zone there — wild risen NPCs live with their wounds).</summary>
        public float NpcGiveUpMinutes { get; init; } = 3f;
    }

    /// <summary>Consumes the Recovery catalog; built-in defaults apply if the JSON is absent.</summary>
    public interface IRecoveryConfigProvider
    {
        RecoveryConfig Config { get; }
    }
}
