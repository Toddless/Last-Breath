namespace Core.Data.AbilityData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>The two sections of the ability file. The keys are named here rather than only in the
    /// authoring tool's descriptor: a reference narrowed to one section is written on a DTO, and the game
    /// cannot see the descriptor.</summary>
    public record AbilityDataRoot
    {
        /// <summary>Json key of the section holding the abilities a fighter casts.</summary>
        public const string AbilitiesSection = "abilities";

        /// <summary>Json key of the section holding the augments written beside them.</summary>
        public const string AugmentsSection = "augments";

        [JsonProperty(AbilitiesSection)] public List<AbilityBaseData> Abilities { get; init; } = [];

        /// <summary>The augment records, in a section of their own. An augment is nobody's property:
        /// which sockets take it follows from its own declaration, so a record written inside one
        /// ability's block would state a belonging no rule reads — and an augment declaring itself at
        /// home on every ability has no such block to be written in at all.</summary>
        [JsonProperty(AugmentsSection)] public List<AbilityAugmentData> Augments { get; init; } = [];
    }
}
