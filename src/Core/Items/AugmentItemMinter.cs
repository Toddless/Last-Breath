namespace Core.Items
{
    using Battle.Abilities;

    /// <summary>
    /// Where an augment enters the world as a thing to own. Two doors, and the difference between
    /// them is the draw: a fresh augment is rolled, while one a save file remembers is put back
    /// exactly as it was written down.
    /// </summary>
    public interface IAugmentItemMinter
    {
        /// <summary>A fresh copy of the augment named, drawn once. Null for an id no record declares —
        /// including every id that is not an augment at all, which is how the item minter asks whether
        /// an id is one.</summary>
        IAugmentItem? Mint(string augmentId);

        /// <summary>The copy a file remembers, carried back into the world without a draw. Null when
        /// the catalog no longer declares the record: an augment whose record left the game cannot be
        /// rebuilt, and inventing one would be a different augment.</summary>
        IAugmentItem? Restore(AugmentInstance augment);
    }

    /// <param name="augments">What an id means. Asked first on both doors: the record is what says
    /// whether the id is an augment at all and where it stands on the rarity scale.</param>
    /// <param name="minter">The one place a copy's numbers are drawn. Handed the record rather than
    /// the id, so an id nobody declares is answered here instead of reported as a miss — the item
    /// minter offers it every id there is.</param>
    public sealed class AugmentItemMinter(IAbilityAugmentCatalog augments, AugmentMinter minter) : IAugmentItemMinter
    {
        public IAugmentItem? Mint(string augmentId) =>
            augments.Find(augmentId) is { } record ? new AugmentItem(minter.Mint(record), record.Rarity) : null;

        public IAugmentItem? Restore(AugmentInstance augment) =>
            augments.Find(augment.AugmentId) is { } record ? new AugmentItem(augment, record.Rarity) : null;
    }
}
