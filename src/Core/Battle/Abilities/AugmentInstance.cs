namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.AbilityData;
    using Enums;

    /// <summary>
    /// One augment as it exists in the world: the record it was made from, and the numbers this copy
    /// came out at. Two copies of one record are not the same augment any more — one rolled 0.225 and
    /// the other 0.375 — so an id alone answers only what an augment is ABOUT, and wherever it matters
    /// which copy is meant, this is what travels.
    ///
    /// The numbers are rolled once, when the copy is minted (<see cref="AugmentMinter"/>), and never
    /// again: a value recomputed on every read would move between two looks at the same tooltip.
    /// </summary>
    /// <param name="augmentId">Stable id of the record this copy was made from.</param>
    /// <param name="values">This copy's own number per property the record declares.</param>
    /// <param name="rarity">Where THIS copy landed on the scale — drawn from the record's band at the
    /// mint, exactly like its numbers, and kept for good.</param>
    /// <param name="effectId">Which effect of the record's pool this copy lays, drawn on the same seam.
    /// Empty for the records that name their effect themselves, which is most of the catalog.</param>
    public sealed class AugmentInstance(
        string augmentId,
        IReadOnlyDictionary<string, float> values,
        Rarity rarity,
        string effectId = "")
    {
        public string AugmentId { get; } = augmentId;

        public IReadOnlyDictionary<string, float> Values { get; } = values;

        /// <summary>What this copy is worth. The copy's own, not the record's: a record rebalanced after
        /// the mint moves neither the numbers nor the rarity of copies already in the world.</summary>
        public Rarity Rarity { get; } = rarity;

        /// <summary>What this copy lays, out of the pool its record offers. Drawn once, like the rarity,
        /// and kept: an effect re-drawn on every read would make two looks at one tooltip disagree.</summary>
        public string EffectId { get; } = effectId;

        /// <summary>
        /// The record as this copy rolled it: the declaration unchanged, with the numbers replaced by
        /// the ones the copy carries. Everything an augment is made of reads that one dictionary — the
        /// behaviour it installs and the description it prints alike — so what the player is shown is
        /// what he gets, whatever this particular copy rolled.
        /// A property the record has gained since the copy was minted keeps its declared base: the copy
        /// says nothing about a number that did not exist when it was rolled.
        /// <para>The drawn effect enters the same way, and brings its own genus with it: a copy that
        /// rolled a regeneration grants recovery where one that rolled a damage buff does not, because
        /// what an applier teaches its ability is the genus of what IT lays.</para>
        /// </summary>
        public AbilityAugmentData Applied(AbilityAugmentData record)
        {
            Dictionary<string, float> properties = new(record.UpgradeProperties);
            foreach ((string property, float value) in Values) properties[property] = value;

            AbilityAugmentData applied = record with { UpgradeProperties = properties };
            if (!record.EffectPool.TryGetValue(EffectId, out string[]? genus)) return applied;

            // The pool goes with the draw: a copy has an effect, not a choice of one, and a record
            // carrying both a named effect and a pool to draw it from states its content twice.
            return applied with
            {
                EffectId = EffectId,
                EffectPool = [],
                GrantsTags = [.. record.GrantsTags.Union(genus, StringComparer.OrdinalIgnoreCase)]
            };
        }
    }
}
