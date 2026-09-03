namespace Core.Data.CombatRulesData
{
    using Core.Data.Schema;
    using Core.Enums;
    using Newtonsoft.Json;

    public record CombatRulesData
    {
        [JsonProperty("controlResistance")] public ControlResistanceData ControlResistance { get; init; } = new();
        [JsonProperty("arena")] public ArenaData Arena { get; init; } = new();
        [JsonProperty("exhaustion")] public ExhaustionData Exhaustion { get; init; } = new();
        [JsonProperty("effects")] public EffectsData Effects { get; init; } = new();
        [JsonProperty("multicast")] public MulticastData Multicast { get; init; } = new();
    }

    /// <summary>The "multicast" section: the intelligence stance activation roll (see MulticastRules).</summary>
    public record MulticastData
    {
        /// <summary>Rolled stages; stage 1 always fires and has no row here.</summary>
        [JsonProperty("stages")] public MulticastStageData[] Stages { get; init; } = [];
    }

    /// <summary>One rolled stage: the stage it decides, its base chance and the ceiling that chance
    /// may be raised to.</summary>
    public record MulticastStageData
    {
        [JsonProperty("stage")] public int Stage { get; init; }

        /// <summary>Chance before the caster's MulticastChance multiplies it. A row naming a share
        /// outside the ends is dropped at load, so the ends are the field's own.</summary>
        [JsonProperty("chance")] [Range(0, 1)] public float Chance { get; init; }

        /// <summary>Ceiling of the final chance; a stage is guaranteed only where its row says 1.</summary>
        [JsonProperty("cap")] [Range(0, 1)] public float Cap { get; init; } = 1f;
    }

    /// <summary>The "effects" section: what holds for every effect instance (see EffectRules).</summary>
    public record EffectsData
    {
        /// <summary>Turns one instance may gain from extensions in total. Balance placeholder.</summary>
        [JsonProperty("maxExtendedTurns")] public int MaxExtendedTurns { get; init; } = 3;
    }

    /// <summary>The "exhaustion" section: per-turn ability-spam limiter (see ExhaustionRules).</summary>
    public record ExhaustionData
    {
        /// <summary>Cost surcharge per stack (0.25 = every stack makes abilities 25% pricier).</summary>
        [JsonProperty("costIncreasePerStack")] public float CostIncreasePerStack { get; init; }

        /// <summary>Stacks forgiven at the bearer's turn end. Generous by design: a typical turn
        /// zeroes out, only a real burst carries over.</summary>
        [JsonProperty("decayPerTurn")] public int DecayPerTurn { get; init; } = int.MaxValue;
    }

    /// <summary>The "arena" section: battle-slot budget of the battlefield.</summary>
    public record ArenaData
    {
        /// <summary>Total battle slots (the player included). Corpse-held slots count until the
        /// battle ends; summon slots live outside this budget.</summary>
        [JsonProperty("maxBattleSlots")] public int MaxBattleSlots { get; init; } = 10;
    }

    /// <summary>Diminishing returns of hard control: every next application of a hard-CC status on a
    /// protected entity is shorter, applications beyond the multiplier list are resisted outright.</summary>
    public record ControlResistanceData
    {
        [JsonProperty("hardControlStatuses")] [EnumOf(typeof(StatusEffects))] public string[] HardControlStatuses { get; init; } = [];

        /// <summary>Duration multiplier per application, in order; past the end = immune.</summary>
        [JsonProperty("durationMultipliers")] public float[] DurationMultipliers { get; init; } = [];

        /// <summary>Entity types the resistance is granted to (Boss, Archon...).</summary>
        [JsonProperty("appliesTo")] [EnumOf(typeof(EntityType))] public string[] AppliesTo { get; init; } = [];

        /// <summary>Turns of the bearer for FULL resistance to fade back to zero.</summary>
        [JsonProperty("resistanceDecayTurns")] public int ResistanceDecayTurns { get; init; } = 7;
    }
}
