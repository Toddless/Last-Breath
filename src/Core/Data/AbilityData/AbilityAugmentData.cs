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
    public record AbilityAugmentData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>What the augment is about. An unbound augment fits an ability sharing one of them.</summary>
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];

        /// <summary>The augment's own tier — how strong it is, not which socket it goes into.</summary>
        [JsonProperty("tier")] public int Tier { get; init; }

        /// <summary>Where the augment stands on the common item scale when it rolls no range. Written
        /// out rather than left to the enum's zero, which is <see cref="Rarity.Legendary"/>: an unstated
        /// rarity is the plainest augment there is, not the best one.</summary>
        [JsonProperty("rarity")] public Rarity Rarity { get; init; } = Rarity.Common;

        /// <summary>The WORST a copy of this augment may roll; absent, the record rolls no range and
        /// every copy comes out at <see cref="Rarity"/>.</summary>
        [JsonProperty("minRarity")] public Rarity? MinRarity { get; init; }

        /// <summary>The BEST a copy may roll. Note the scale runs downward — <see cref="Rarity.Legendary"/>
        /// is zero — so the best end is the smaller number.</summary>
        [JsonProperty("maxRarity")] public Rarity? MaxRarity { get; init; }

        /// <summary>The band a copy is drawn from, worst end first. A record without a range is a band
        /// of one, so every reader asks one question instead of two.</summary>
        public (Rarity Worst, Rarity Best) RarityBand => (MinRarity ?? Rarity, MaxRarity ?? Rarity);

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

        /// <summary>Id of the mutual-exclusion group: an ability wears at most one augment of a group.
        /// Empty conflicts with nothing.</summary>
        [JsonProperty("exclusionGroup")] public string ExclusionGroup { get; init; } = string.Empty;

        /// <summary>Tags the augment, once installed, grants its ability for fitting FURTHER augments —
        /// the genus of the content it lays (a poison applier grants "poison"). Declared only by
        /// appliers: amplifiers grant nothing, or they would bootstrap each other without one.</summary>
        [JsonProperty("grantsTags")] public string[] GrantsTags { get; init; } = [];

        /// <summary>What the augment DOES, named out of the behaviour registry — the road that needs no
        /// code of its own. Empty means the augment is one of the named ones and a factory answers its id.</summary>
        [JsonProperty("behaviour")] public string Behaviour { get; init; } = string.Empty;

        /// <summary>Effect the behaviour lays, built by the effect registry from
        /// <see cref="UpgradeProperties"/>. Read by the behaviours that lay one.</summary>
        [JsonProperty("effectId")] public string EffectId { get; init; } = string.Empty;

        /// <summary>Which touches the behaviour works on: "Attack", "Hit", "Projectile", "ChainJump",
        /// "Splash", or empty for every one of them. The design list tells attacks from hits.</summary>
        [JsonProperty("impactKind")] public string ImpactKind { get; init; } = string.Empty;

        /// <summary>Attack modifier the behaviour installs, named out of the modifier registry.</summary>
        [JsonProperty("attackModifier")] public string AttackModifier { get; init; } = string.Empty;

        /// <summary>Properties whose value is taken from the HOST ability instead of the record: property
        /// key to a shared <see cref="Battle.Abilities.AbilityParameter"/> name, read decorated at the
        /// moment of use. The record still carries a number for each — what the ability does not own it
        /// falls back to.</summary>
        [JsonProperty("propertyRefs")] public Dictionary<string, string> PropertyRefs { get; init; } = [];

        [JsonProperty("upgradeProperties")] public Dictionary<string, float> UpgradeProperties { get; init; } = [];

        /// <summary>
        /// The far end of a design line that states a RANGE: what the property is worth at the BEST
        /// rarity the record rolls, where <see cref="UpgradeProperties"/> gives it at the worst. A
        /// record whose line names one number writes nothing here and every rarity is worth the same —
        /// which is most of the catalog, so the field stays absent rather than repeating the figure.
        /// Only properties the line puts a range on belong here; durations and stack counts the line
        /// states flatly are not ladders. See <c>Docs/PLAN-Augments.md §4g</c>.
        /// </summary>
        [JsonProperty("bestRarityProperties")] public Dictionary<string, float> BestRarityProperties { get; init; } = [];

        /// <summary>
        /// The rungs written out by hand, worst rarity first, one per rarity of the band. For lines the
        /// even interpolation reads badly on — a narrow share ladder rounds two rarities onto one figure
        /// — the author names every step instead, and nothing is interpolated or rounded. A record
        /// without one keeps the two-ends path.
        /// </summary>
        [JsonProperty("rarityLadder")] public Dictionary<string, float[]> RarityLadder { get; init; } = [];

        /// <summary>What one property is worth at the two ends of the band — the far end when the
        /// record names one, the same figure twice when it does not.</summary>
        public (float AtWorst, float AtBest) Ends(string property)
        {
            float atWorst = UpgradeProperties.GetValueOrDefault(property);
            return (atWorst, BestRarityProperties.GetValueOrDefault(property, atWorst));
        }

        /// <summary>The author's own rungs for a property, or null where there are none.</summary>
        public IReadOnlyList<float>? AuthoredRungs(string property) =>
            RarityLadder.TryGetValue(property, out float[]? rungs) && rungs.Length > 0 ? rungs : null;
    }
}
