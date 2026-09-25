namespace Core.Results
{
    /// <summary>
    /// Which gate a conversion stopped at. Every answer but <see cref="Converted"/> leaves the bag
    /// exactly as it was: three copies are handed over only once the thing they buy is already in the
    /// player's hands, so a refusal is never paid for.
    /// </summary>
    public enum AugmentConversionOutcome : byte
    {
        /// <summary>The three copies are gone and one fresh augment lies in the bag in their place.</summary>
        Converted,

        /// <summary>The conversion was not offered three separate copies. Three ids naming the same
        /// copy twice are two augments, not three, and would buy an augment for the price of two.</summary>
        NotThreeAugments,

        /// <summary>The bag holds no augment under one of the ids. Nothing moved.</summary>
        AugmentNotHeld,

        /// <summary>One of the three names a record the catalog no longer declares. What tier and
        /// rarity it belongs to is written in that record and nowhere else, so a conversion that went
        /// ahead would be trading it into a class it may never have belonged to.</summary>
        UndeclaredAugment,

        /// <summary>The three are not of one tier. Tier is the whole of what an augment is worth, and
        /// a mixed handful would let the weakest two pull a strong one down or up.</summary>
        MixedTiers,

        /// <summary>The three are not of one rarity, for the same reason the tiers must match.</summary>
        MixedRarities,

        /// <summary>Nothing at that tier and rarity is left for the conversion to give back: every
        /// record of the class is one of the three handed over. The copies stay in the bag rather than
        /// being spent on a draw with nothing in it.</summary>
        NothingToGiveBack,

        /// <summary>The bag has no room for what the conversion would hand back. The three stay where
        /// they are: an augment's numbers are drawn once, and three of them must never be spent on a
        /// result that has nowhere to land.</summary>
        NoBagRoom
    }

    /// <summary>
    /// What came of turning three augments into one. A conversion never improves what it is given —
    /// the result is drawn fresh at the tier and rarity of the three, and inherits none of their
    /// numbers — so it is the place to put augments that are of no use, not a second way to build one.
    /// </summary>
    /// <param name="Outcome">Which gate answered.</param>
    /// <param name="ProducedItemInstanceId">The copy the bag received, by the id it is held under.
    /// Null for every refusal, since nothing was produced.</param>
    public readonly record struct AugmentConversionResult(AugmentConversionOutcome Outcome, string? ProducedItemInstanceId = null)
    {
        public bool Converted => Outcome == AugmentConversionOutcome.Converted;
    }
}
