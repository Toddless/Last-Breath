namespace Core.Data.WorldData
{
    using Newtonsoft.Json;

    /// <summary>Data/Player/PlayerLifecycle.json (own catalog: every json of a catalog goes to
    /// the same participant, so it must not share one with WorldClock.json).</summary>
    public class PlayerLifecycleData
    {
        [JsonProperty("lieGameHours")] public float LieGameHours { get; init; } = 4f;
        [JsonProperty("reviveHealthPercent")] public float ReviveHealthPercent { get; init; } = 0.1f;
        [JsonProperty("reviveManaPercent")] public float ReviveManaPercent { get; init; } = 0.1f;
        [JsonProperty("deadTimeScale")] public float DeadTimeScale { get; init; } = 8f;
        [JsonProperty("burnChance")] public float BurnChance { get; init; } = 0.15f;
        [JsonProperty("burnRadius")] public float BurnRadius { get; init; } = 120f;
    }
}
