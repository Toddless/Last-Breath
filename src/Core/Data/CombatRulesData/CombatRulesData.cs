namespace Core.Data.CombatRulesData
{
    using Newtonsoft.Json;

    public record CombatRulesData
    {
        [JsonProperty("controlResistance")] public ControlResistanceData ControlResistance { get; init; } = new();
        [JsonProperty("arena")] public ArenaData Arena { get; init; } = new();
        [JsonProperty("exhaustion")] public ExhaustionData Exhaustion { get; init; } = new();
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
        [JsonProperty("hardControlStatuses")] public string[] HardControlStatuses { get; init; } = [];

        /// <summary>Duration multiplier per application, in order; past the end = immune.</summary>
        [JsonProperty("durationMultipliers")] public float[] DurationMultipliers { get; init; } = [];

        /// <summary>Entity types the resistance is granted to (Boss, Archon...).</summary>
        [JsonProperty("appliesTo")] public string[] AppliesTo { get; init; } = [];

        /// <summary>Turns of the bearer for FULL resistance to fade back to zero.</summary>
        [JsonProperty("resistanceDecayTurns")] public int ResistanceDecayTurns { get; init; } = 7;
    }
}
