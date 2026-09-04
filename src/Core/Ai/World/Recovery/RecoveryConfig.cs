namespace Core.Ai.World.Recovery
{
    using Newtonsoft.Json;

    /// <summary>
    /// Rest tuning (the Recovery catalog). Rates are shares of the maximum restored per GAME
    /// minute inside a recovery zone (campfire, spawn point); defaults are made-up placeholders.
    /// </summary>
    /// <remarks>The type the file is parsed into as well as the one the game reads: a second record
    /// written field for field would let a field reach one of them and not the other.</remarks>
    public class RecoveryConfig
    {
        [JsonProperty("healthPercentPerMinute")] public float HealthPercentPerMinute { get; init; } = 0.1f;
        [JsonProperty("manaPercentPerMinute")] public float ManaPercentPerMinute { get; init; } = 0.15f;
        [JsonProperty("barrierPercentPerMinute")] public float BarrierPercentPerMinute { get; init; } = 0.15f;

        /// <summary>Zone radius used when the registering node does not supply its own (campfire).</summary>
        [JsonProperty("defaultZoneRadius")] public float DefaultZoneRadius { get; init; } = 150f;

        /// <summary>NPC abandons its routine and heads home to rest below this health share.</summary>
        [JsonProperty("npcRetreatHealthPercent")] public float NpcRetreatHealthPercent { get; init; } = 0.5f;

        /// <summary>Resting at home without any health gain for this many game minutes = give up
        /// (no recovery zone there — wild risen NPCs live with their wounds).</summary>
        [JsonProperty("npcGiveUpMinutes")] public float NpcGiveUpMinutes { get; init; } = 3f;
    }

    /// <summary>Consumes the Recovery catalog; built-in defaults apply if the JSON is absent.</summary>
    public interface IRecoveryConfigProvider
    {
        RecoveryConfig Config { get; }
    }
}
