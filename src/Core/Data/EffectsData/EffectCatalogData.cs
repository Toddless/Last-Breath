namespace Core.Data.EffectsData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>The SharedData/Effects catalog: the canonical numbers of every temporary effect.</summary>
    public record EffectCatalogData
    {
        [JsonProperty("effects")] public IReadOnlyList<EffectDefinitionData> Effects { get; init; } = [];
    }

    /// <summary>One effect's canonical numbers. Keys match what the registry factory declares —
    /// duration and the stack ceiling included, so a record and the canon speak the same names.</summary>
    public record EffectDefinitionData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>Optional strength against dispelling (<c>EffectPower</c>); absent means Weak.
        /// A string beside the numbers rather than among them: the properties are figures a factory reads.</summary>
        [JsonProperty("power")] public string? Power { get; init; }

        [JsonProperty("properties")] public IReadOnlyDictionary<string, float> Properties { get; init; } =
            new Dictionary<string, float>();
    }
}
