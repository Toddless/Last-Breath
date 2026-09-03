namespace Core.Data.AbilityData
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    /// <summary>
    /// One augment as its own record declares it. Every field describes the augment itself and never
    /// the slot it ends up in: which sockets accept it follows from its tier, its tags and its
    /// binding, and the rule that reads them is code, not data.
    /// </summary>
    public record AbilityAugmentData
    {
        /// <summary>The placeholder a pool record's description prints its drawn effect under — the one
        /// description value that is not a property of the record, so bag and socket print it alike.</summary>
        public const string EffectPlaceholder = "effect";

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
        [JsonProperty("abilityId")][CatalogRef(DataCatalog.Abilities, AllowEmpty = true)] public string AbilityId { get; init; } = string.Empty;

        /// <summary>Declares the augment at home on every ability — for records working through the base
        /// contract (cost, cooldown). Universality must be CLAIMED: a record naming no tag is simply
        /// refused. Only the binding question is answered here; tier and exclusion group still apply.
        /// Claiming this and naming an <see cref="AbilityId"/> is refused outright
        /// (<see cref="Battle.Abilities.AugmentFitResult.ContradictoryDeclaration"/>).</summary>
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
        [JsonProperty("behaviour")][NotARef] public string Behaviour { get; init; } = string.Empty;

        /// <summary>Effect the behaviour lays, built by the effect registry from
        /// <see cref="UpgradeProperties"/>. Read by the behaviours that lay one.</summary>
        [JsonProperty("effectId")][CatalogRef(DataCatalog.Effects, AllowEmpty = true)] public string EffectId { get; init; } = string.Empty;

        /// <summary>The effects a copy of this record may turn out to lay, and the genus each adds to
        /// <see cref="GrantsTags"/>. Which one a copy lays is drawn at the mint and belongs to the COPY.
        /// Written INSTEAD of <see cref="EffectId"/> and never beside it.</summary>
        [JsonProperty("effectPool")][DictionaryKey(DataCatalog.Effects)] public Dictionary<string, string[]> EffectPool { get; init; } = [];

        /// <summary>The pool in the order the draw walks it — ordinal, so which effect an index means is
        /// decided by the ids themselves and not by where a hand put a line in the file.</summary>
        /// <remarks>Said to be no part of the file out loud: a get-only COLLECTION is what a reader pours
        /// values into rather than something worked out, so a walk reading the type would take it for a
        /// key an author writes.</remarks>
        [JsonIgnore] public IReadOnlyList<string> PoolEffects => [.. EffectPool.Keys.Order(StringComparer.Ordinal)];

        /// <summary>The effect the record lays when no copy has been drawn — its own, or the first of its
        /// pool: a representative, which is all a declaration alone can offer.</summary>
        public string LaidEffectId =>
            !string.IsNullOrWhiteSpace(EffectId) ? EffectId : PoolEffects.FirstOrDefault() ?? string.Empty;

        /// <summary>Every tag a copy of this record MIGHT grant. The copy grants what IT rolled
        /// (<see cref="Battle.Abilities.AugmentInstance.Applied"/>); this is the union across the pool,
        /// which is what a ledger of reachable board states has to measure.</summary>
        /// <remarks>Kept out of the file for the reason <see cref="PoolEffects"/> is.</remarks>
        [JsonIgnore] public IReadOnlyCollection<string> GrantableTags =>
            [.. GrantsTags.Concat(EffectPool.Values.SelectMany(tags => tags)).Distinct(StringComparer.OrdinalIgnoreCase)];

        /// <summary>Which touches the behaviour works on: "Attack", "Hit", "Projectile", "ChainJump",
        /// "Splash", or empty for every one of them. The design list tells attacks from hits.</summary>
        [JsonProperty("impactKind")][EnumOf(typeof(Data.ImpactKind))] public string ImpactKind { get; init; } = string.Empty;

        /// <summary>Attack modifier the behaviour installs, named out of the modifier registry.</summary>
        [JsonProperty("attackModifier")][NotARef] public string AttackModifier { get; init; } = string.Empty;

        /// <summary>Says the damage-over-time effect this record lays pools the WHOLE blow instead of the
        /// component its kind feeds on — what a record declares when it burns for a share of any hit rather
        /// than for a share of the fire in one. Absent, the effect's own kind decides, as it does everywhere.</summary>
        [JsonProperty("poolFromWholeHit")] public bool PoolFromWholeHit { get; init; }

        /// <summary>Properties whose value is taken from the HOST ability instead of the record: property
        /// key to a shared <see cref="Battle.Abilities.AbilityParameter"/> name, read decorated at the
        /// moment of use. The record still carries a number for each — what the ability does not own it
        /// falls back to.</summary>
        [JsonProperty("propertyRefs")][NotARef] public Dictionary<string, string> PropertyRefs { get; init; } = [];

        [JsonProperty("upgradeProperties")] public Dictionary<string, float> UpgradeProperties { get; init; } = [];

        /// <summary>What a ranged property is worth at the BEST rarity the record rolls;
        /// <see cref="UpgradeProperties"/> gives it at the worst. Absent means every rarity is worth the
        /// same. Only properties the design line puts a range on belong here — flatly stated durations and
        /// stack counts are not ladders. See <c>Docs/PLAN-Augments.md §4g</c>.</summary>
        [JsonProperty("bestRarityProperties")] public Dictionary<string, float> BestRarityProperties { get; init; } = [];

        /// <summary>Rungs written out by hand, worst rarity first, one per rarity of the band — for lines
        /// even interpolation reads badly on. Nothing here is interpolated or rounded; a record without one
        /// keeps the two-ends path.</summary>
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
