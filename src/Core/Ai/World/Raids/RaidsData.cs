namespace Core.Ai.World.Raids
{
    using Newtonsoft.Json;

    /// <summary>Raids.json: when and how factions at Hatred standing raid the player.</summary>
    public record RaidsData
    {
        /// <summary>Grace after a session start before the first raid may roll.</summary>
        [JsonProperty("initialDelaySeconds")] public float InitialDelaySeconds { get; init; } = 180f;

        [JsonProperty("checkIntervalSeconds")] public float CheckIntervalSeconds { get; init; } = 30f;

        /// <summary>Per-check roll once the cooldown is over.</summary>
        [JsonProperty("chance")] public float Chance { get; init; } = 0.35f;

        /// <summary>After a raid ends, no faction raids again for this long.</summary>
        [JsonProperty("cooldownSeconds")] public float CooldownSeconds { get; init; } = 900f;

        /// <summary>Raiders that failed to reach the player leave after this long.</summary>
        [JsonProperty("durationSeconds")] public float DurationSeconds { get; init; } = 240f;

        [JsonProperty("squadSizeMin")] public int SquadSizeMin { get; init; } = 2;
        [JsonProperty("squadSizeMax")] public int SquadSizeMax { get; init; } = 4;

        [JsonProperty("moveSpeed")] public float MoveSpeed { get; init; } = 220f;

        /// <summary>Scatter around the spawn site so the squad doesn't stack on one pixel.</summary>
        [JsonProperty("spawnJitter")] public float SpawnJitter { get; init; } = 120f;
    }
}
