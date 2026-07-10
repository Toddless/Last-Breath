namespace Core.Data.ReputationData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>ReputationDeeds.json: what each player deed does to faction standings.</summary>
    public record ReputationDeedsData
    {
        /// <summary>How far a bystander NPC can be from the deed and still learn about it.</summary>
        [JsonProperty("witnessRadius")] public float WitnessRadius { get; init; } = 600f;

        [JsonProperty("deeds")] public List<ReputationDeedEntry> Deeds { get; init; } = [];
    }

    public record ReputationDeedEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>Points applied to the target faction's standing.</summary>
        [JsonProperty("reputation")] public int Reputation { get; init; }

        /// <summary>Points applied to the target NPC's PERSONAL opinion (needs a target instance; pointless for kills).</summary>
        [JsonProperty("personal")] public int Personal { get; init; }

        /// <summary>Anti-death-spiral floor: a negative deed costs nothing when the player's standing
        /// with the target faction is already at or below this level (killing enemies is fair game).</summary>
        [JsonProperty("noPenaltyAtOrBelow")] public string? NoPenaltyAtOrBelow { get; init; }

        /// <summary>Factions whose matrix relation TO the target faction is hostile approve — each gets this many points.</summary>
        [JsonProperty("hostileToTargetBonus")] public int HostileToTargetBonus { get; init; }

        /// <summary>The deed counts only if somebody learned about it (see IWitnessQuery).</summary>
        [JsonProperty("requiresWitness")] public bool RequiresWitness { get; init; }

        /// <summary>Anti-farm: each repetition multiplies the per-faction delta (1 = no decay). Session-scoped.</summary>
        [JsonProperty("repeatDecay")] public float RepeatDecay { get; init; } = 1f;
    }
}
