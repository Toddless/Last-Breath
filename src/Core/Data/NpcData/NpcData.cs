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

        /// <summary>Roll ceiling override; absent = the EntityType cap. levelMax == levelMin pins the level (bosses).</summary>
        [JsonProperty("levelMax")] public int? LevelMax { get; init; }

        /// <summary>Per-level fraction added to every base parameter: value * (1 + (level - 1) * levelScaling).</summary>
        [JsonProperty("levelScaling")] public float LevelScaling { get; init; }

        /// <summary>Learned ability count; 0 = derive from entity type (2/3/4/5, bosses take all).</summary>
        [JsonProperty("abilityCount")] public int AbilityCount { get; init; }

        /// <summary>Fixed rarity name (bosses/uniques). Empty = rolled by weight at spawn.</summary>
        [JsonProperty("rarity")] public string? Rarity { get; init; }

        /// <summary>Authored ability list (bosses). Non-empty = exactly these instead of the archetype roll.</summary>
        [JsonProperty("abilities")] public List<string> Abilities { get; init; } = [];

        /// <summary>Per-NPC override of the archetype's flee threshold; 0 = never flees (bosses).</summary>
        [JsonProperty("fleeHealthThreshold")] public float? FleeHealthThreshold { get; init; }

        /// <summary>Per-NPC planner entries merged OVER the stance archetype: how the combat AI
        /// scores authored abilities that live outside the archetype pool (boss kits).</summary>
        [JsonProperty("abilityBehaviors")] public List<NpcAbilityBehaviorData> AbilityBehaviors { get; init; } = [];

        /// <summary>Combat reactions (hidden triggered casts); a broken entry is reported and skipped.</summary>
        [JsonProperty("reactions")] public List<NpcReactionData> Reactions { get; init; } = [];

        /// <summary>Boss stages; a broken entry drops the whole section (see NpcStageParser).</summary>
        [JsonProperty("stages")] public List<NpcStageData> Stages { get; init; } = [];

        [JsonProperty("baseParameters")] public Dictionary<string, float> BaseParameters { get; init; } = [];

        /// <summary>World behavior (perception, movement, activity). Null = static NPC (no world brain).</summary>
        [JsonProperty("world")] public NpcWorldData? World { get; init; }

        /// <summary>Post-defeat rules override. Null = design defaults (rise in 1–10 minutes).</summary>
        [JsonProperty("lifecycle")] public NpcLifecycleData? Lifecycle { get; init; }

        /// <summary>Species capabilities (talking, later trading). Null = can do none of it.</summary>
        [JsonProperty("interaction")] public NpcInteractionData? Interaction { get; init; }
    }

    /// <summary>The "interaction" section: what the SPECIES is capable of. Hostility is state
    /// (reputation), never declared here — a hostile veteran may talk, a friendly wolf never will.</summary>
    public record NpcInteractionData
    {
        [JsonProperty("canTalk")] public bool CanTalk { get; init; }
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
        [JsonProperty("hostileToPlayer")] public bool HostileToPlayer { get; init; }
        [JsonProperty("activity")] public string Activity { get; init; } = "Idle";
        [JsonProperty("wanderRadius")] public float WanderRadius { get; init; } = 250f;
        [JsonProperty("activityPauseSeconds")] public float ActivityPauseSeconds { get; init; } = 2f;

        /// <summary>Sleeping senses: vision/hearing radii scaled down while the Sleep pose holds.</summary>
        [JsonProperty("sleepVisionMultiplier")] public float SleepVisionMultiplier { get; init; } = 0.3f;

        [JsonProperty("sleepHearingMultiplier")] public float SleepHearingMultiplier { get; init; } = 0.6f;

        /// <summary>Daily routine: "HH:MM" windows switching the Calm activity; gaps fall back to "activity".</summary>
        [JsonProperty("schedule")] public List<NpcScheduleSlotData> Schedule { get; init; } = [];

        /// <summary>Duration-driven routine: timed steps cycled in order. Non-empty wins over "schedule".</summary>
        [JsonProperty("routine")] public List<NpcRoutineStepData> Routine { get; init; } = [];
    }

    /// <summary>One routine step: the activity runs for a budget of game minutes, then the cycle advances.</summary>
    public record NpcRoutineStepData
    {
        [JsonProperty("activity")] public string Activity { get; init; } = "Idle";
        [JsonProperty("minutes")] public float Minutes { get; init; } = 60f;
        [JsonProperty("wanderRadius")] public float? WanderRadius { get; init; }

        /// <summary>Smart point tag the activity anchors to (Campfire/Tent/OreVein/...); null = the type's default.</summary>
        [JsonProperty("point")] public string? Point { get; init; }
    }

    /// <summary>One schedule window; 22:00–06:00 style entries wrap through midnight.</summary>
    public record NpcScheduleSlotData
    {
        [JsonProperty("from")] public string From { get; init; } = "00:00";
        [JsonProperty("to")] public string To { get; init; } = "00:00";
        [JsonProperty("activity")] public string Activity { get; init; } = "Idle";
        [JsonProperty("wanderRadius")] public float? WanderRadius { get; init; }

        /// <summary>Smart point tag the activity anchors to (Campfire/Tent/OreVein/...); null = the type's default.</summary>
        [JsonProperty("point")] public string? Point { get; init; }
    }

    /// <summary>One "reactions" entry — parsed strictly into NpcReactionConfig by NpcReactionParser.</summary>
    public record NpcReactionData
    {
        [JsonProperty("abilityId")] public string AbilityId { get; init; } = string.Empty;
        [JsonProperty("trigger")] public string Trigger { get; init; } = string.Empty;
        [JsonProperty("chance")] public float Chance { get; init; }
        [JsonProperty("maxPerTurn")] public int MaxPerTurn { get; init; } = 1;

        /// <summary>Npc id whose final death (world fact) permanently disables this reaction.</summary>
        [JsonProperty("blockedByFinalDeathOf")] public string? BlockedByFinalDeathOf { get; init; }
    }

    /// <summary>One "stages" entry — parsed strictly into NpcStageConfig by NpcStageParser.</summary>
    public record NpcStageData
    {
        [JsonProperty("parameterMultiplier")] public float ParameterMultiplier { get; init; } = 1f;

        /// <summary>The stage's full ability set — replaces the book's content on stage entry.</summary>
        [JsonProperty("abilities")] public List<string> Abilities { get; init; } = [];

        /// <summary>Effects riding every landed attack while this stage is active.</summary>
        [JsonProperty("attackEffects")] public List<NpcStageAttackEffectData> AttackEffects { get; init; } = [];

        /// <summary>Health share arming the one-way transition to the next stage. Absent = final stage.</summary>
        [JsonProperty("nextStageAtHealthPercent")] public float? NextStageAtHealthPercent { get; init; }

        /// <summary>Health share arming the one-time rage burst; requires <see cref="RageBonus"/>.</summary>
        [JsonProperty("rageAtHealthPercent")] public float? RageAtHealthPercent { get; init; }

        /// <summary>Increase bonus (0.25 = +25%) to Damage/CriticalChance/AdditionalHitChance until the battle ends.</summary>
        [JsonProperty("rageBonus")] public float? RageBonus { get; init; }
    }

    /// <summary>One on-attack effect of a boss stage; damagePercent is the effect's magnitude.</summary>
    public record NpcStageAttackEffectData
    {
        [JsonProperty("effect")] public string Effect { get; init; } = string.Empty;
        [JsonProperty("chance")] public float Chance { get; init; }
        [JsonProperty("damagePercent")] public float? DamagePercent { get; init; }
        [JsonProperty("duration")] public int Duration { get; init; }
        [JsonProperty("maxStacks")] public int MaxStacks { get; init; }
    }

    public record NpcsData
    {
        [JsonProperty("npcs")] public List<NpcData> Npcs { get; init; } = [];
    }
}
