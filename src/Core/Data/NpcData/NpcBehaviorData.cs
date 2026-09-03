namespace Core.Data.NpcData
{
    using System.Collections.Generic;
    using Ai;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>
    /// One behavior archetype from NpcBehavior.json (aggressive/defensive/mixed, keyed to a stance).
    /// Turns into a runtime Core.Ai.BehaviorProfile; per-NPC fields (intellect) are merged in by the provider.
    /// </summary>
    public record NpcBehaviorData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        /// <summary>The stance this archetype answers for; the provider keys the archetypes by it, so
        /// two records naming one stance leave only the last one reachable.</summary>
        [JsonProperty("stance")][EnumOf(typeof(Stance))] public string Stance { get; init; } = string.Empty;
        [JsonProperty("maxCastsPerTurn")] public int MaxCastsPerTurn { get; init; } = 1;
        [JsonProperty("temperature")] public float Temperature { get; init; } = 0.15f;
        [JsonProperty("aggression")] public float Aggression { get; init; } = 1f;
        [JsonProperty("caution")] public float Caution { get; init; } = 1f;
        [JsonProperty("greed")] public float Greed { get; init; } = 1f;
        [JsonProperty("castScoreThreshold")] public float CastScoreThreshold { get; init; } = 0.35f;
        [JsonProperty("fleeHealthThreshold")] public float FleeHealthThreshold { get; init; }
        [JsonProperty("abilities")] public List<NpcAbilityBehaviorData> Abilities { get; init; } = [];
    }

    public record NpcAbilityBehaviorData
    {
        [JsonProperty("id")][CatalogRef(DataCatalog.Abilities)] public string Id { get; init; } = string.Empty;
        [JsonProperty("weight")] public float Weight { get; init; } = 1f;
        [JsonProperty("role")][EnumOf(typeof(AbilityRole))] public string Role { get; init; } = string.Empty;
    }

    public record NpcBehaviorsData
    {
        [JsonProperty("behaviors")] public List<NpcBehaviorData> Behaviors { get; init; } = [];
    }
}
