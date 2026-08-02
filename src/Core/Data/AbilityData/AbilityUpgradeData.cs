namespace Core.Data.AbilityData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// One augment as its own record declares it. Every field describes the augment itself and never
    /// the slot it ends up in: which sockets accept it follows from its tier, its tags and its
    /// binding, and the rule that reads them is code, not data.
    /// </summary>
    public record AbilityUpgradeData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>What the augment is about. An unbound augment fits an ability sharing one of them.</summary>
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];

        /// <summary>The augment's own tier — how strong it is, not which socket it goes into.</summary>
        [JsonProperty("tier")] public int Tier { get; init; }

        /// <summary>Where the augment stands on the common item scale. Written out rather than left to
        /// the enum's zero, which is <see cref="Rarity.Legendary"/>: an unstated rarity is the plainest
        /// augment there is, not the best one.</summary>
        [JsonProperty("rarity")] public Rarity Rarity { get; init; } = Rarity.Common;

        /// <summary>Hard binding to a single ability — the exception kept for augments too strong to be
        /// handed to a whole family. Set, it decides alone; empty, the augment is bound to nothing.</summary>
        [JsonProperty("abilityId")] public string AbilityId { get; init; } = string.Empty;

        /// <summary>Declares the augment at home on every ability there is — what a record does when it
        /// works through the base contract every ability already honours, cost and cooldown being the
        /// pair that turns up most. It is a word of its own because no tag is carried by the whole
        /// book, and because universality has to be claimed: a record that simply names no tag is an
        /// augment whose author forgot to say what it is about, and it stays refused. Only the binding
        /// question is answered here — a universal augment is measured for tier and for the exclusion
        /// group its ability already wears exactly like any other. Claiming this and naming an
        /// <see cref="AbilityId"/> answers the same question twice, which no reading resolves; the
        /// fitting rule refuses such a record outright (<see cref="Battle.Abilities.AugmentFitResult.ContradictoryDeclaration"/>).</summary>
        [JsonProperty("fitsAnyAbility")] public bool FitsAnyAbility { get; init; }

        /// <summary>Id of the pool the augment drops from. Empty means it never drops.</summary>
        [JsonProperty("dropPool")] public string DropPool { get; init; } = string.Empty;

        /// <summary>Id of the mutual-exclusion group: an ability wears at most one augment of a group.
        /// Empty conflicts with nothing.</summary>
        [JsonProperty("exclusionGroup")] public string ExclusionGroup { get; init; } = string.Empty;

        [JsonProperty("upgradeProperties")] public Dictionary<string, float> UpgradeProperties { get; init; } = [];
    }
}
