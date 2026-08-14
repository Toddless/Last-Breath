namespace Core.Data.AbilityData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record AbilityDataRoot
    {
        [JsonProperty("abilities")] public List<AbilityBaseData> Abilities { get; init; } = [];

        /// <summary>The augment records, in a section of their own. An augment is nobody's property:
        /// which sockets take it follows from its own declaration, so a record written inside one
        /// ability's block would state a belonging no rule reads — and an augment declaring itself at
        /// home on every ability has no such block to be written in at all.</summary>
        [JsonProperty("augments")] public List<AbilityAugmentData> Augments { get; init; } = [];
    }
}
