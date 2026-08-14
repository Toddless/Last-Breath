namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
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
    public sealed class AugmentInstance(string augmentId, IReadOnlyDictionary<string, float> values, Rarity rarity)
    {
        public string AugmentId { get; } = augmentId;

        public IReadOnlyDictionary<string, float> Values { get; } = values;

        /// <summary>What this copy is worth. The copy's own, not the record's: a record rebalanced after
        /// the mint moves neither the numbers nor the rarity of copies already in the world.</summary>
        public Rarity Rarity { get; } = rarity;

        /// <summary>
        /// The record as this copy rolled it: the declaration unchanged, with the numbers replaced by
        /// the ones the copy carries. Everything an augment is made of reads that one dictionary — the
        /// behaviour it installs and the description it prints alike — so what the player is shown is
        /// what he gets, whatever this particular copy rolled.
        /// A property the record has gained since the copy was minted keeps its declared base: the copy
        /// says nothing about a number that did not exist when it was rolled.
        /// </summary>
        public AbilityAugmentData Applied(AbilityAugmentData record)
        {
            Dictionary<string, float> properties = new(record.UpgradeProperties);
            foreach ((string property, float value) in Values) properties[property] = value;

            return record with { UpgradeProperties = properties };
        }
    }
}
