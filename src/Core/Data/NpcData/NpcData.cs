namespace Core.Data.NpcData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// One NPC definition from Npc.json. Enums travel as names (fraction, entityType, stances)
    /// and are parsed by the provider; base parameters are keyed by EntityParameter name and
    /// replace the hardcoded random roll of BaseNpc.
    /// </summary>
    public record NpcData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("fraction")] public string Fraction { get; init; } = string.Empty;
        [JsonProperty("entityType")] public string EntityType { get; init; } = string.Empty;
        [JsonProperty("aiIntellect")] public string AiIntellect { get; init; } = string.Empty;

        /// <summary>Stances this NPC may roll at spawn; the behavior archetype follows the stance.</summary>
        [JsonProperty("stances")] public List<string> Stances { get; init; } = [];

        [JsonProperty("levelMin")] public int LevelMin { get; init; } = 1;

        /// <summary>Per-level fraction added to every base parameter: value * (1 + (level - 1) * levelScaling).</summary>
        [JsonProperty("levelScaling")] public float LevelScaling { get; init; }

        /// <summary>Learned ability count; 0 = derive from entity type (2/3/4/5, bosses take all).</summary>
        [JsonProperty("abilityCount")] public int AbilityCount { get; init; }

        [JsonProperty("baseParameters")] public Dictionary<string, float> BaseParameters { get; init; } = [];

        /// <summary>World behavior (perception, movement, activity). Null = static NPC (no world brain).</summary>
        [JsonProperty("world")] public NpcWorldData? World { get; init; }

        /// <summary>Post-defeat rules override. Null = design defaults (rise in 1–10 minutes).</summary>
        [JsonProperty("lifecycle")] public NpcLifecycleData? Lifecycle { get; init; }
    }

    /// <summary>The "lifecycle" section — maps 1:1 to Core.Ai.World.NpcLifecycleConfig.</summary>
    public record NpcLifecycleData
    {
        [JsonProperty("resurrectMinSeconds")] public float ResurrectMinSeconds { get; init; } = 60f;
        [JsonProperty("resurrectMaxSeconds")] public float ResurrectMaxSeconds { get; init; } = 600f;
        [JsonProperty("maxStrengthBonus")] public float MaxStrengthBonus { get; init; } = 1f;
    }

    /// <summary>The "world" section of an NPC definition — maps 1:1 to Core.Ai.World.WorldBrainConfig.</summary>
    public record NpcWorldData
    {
        [JsonProperty("visionRadius")] public float VisionRadius { get; init; } = 350f;
        [JsonProperty("hearingRadius")] public float HearingRadius { get; init; } = 600f;
        [JsonProperty("leashRadius")] public float LeashRadius { get; init; } = 900f;
        [JsonProperty("moveSpeed")] public float MoveSpeed { get; init; } = 120f;
        [JsonProperty("chaseSpeedMultiplier")] public float ChaseSpeedMultiplier { get; init; } = 1.6f;
        [JsonProperty("suspiciousSeconds")] public float SuspiciousSeconds { get; init; } = 4f;
        [JsonProperty("searchSeconds")] public float SearchSeconds { get; init; } = 5f;
        [JsonProperty("postBattleGraceSeconds")] public float PostBattleGraceSeconds { get; init; } = 5f;
        [JsonProperty("aggressive")] public bool Aggressive { get; init; } = true;
        [JsonProperty("activity")] public string Activity { get; init; } = "Idle";
        [JsonProperty("wanderRadius")] public float WanderRadius { get; init; } = 250f;
        [JsonProperty("activityPauseSeconds")] public float ActivityPauseSeconds { get; init; } = 2f;
    }

    public record NpcsData
    {
        [JsonProperty("npcs")] public List<NpcData> Npcs { get; init; } = [];
    }
}
