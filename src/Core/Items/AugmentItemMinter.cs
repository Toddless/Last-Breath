namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using Battle.Abilities;
    using Enums;

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
        /// <param name="rarity">What the copy is worth, when somebody has already decided — a loot
        /// table seat bought as the Rare one. Null leaves the rarity to the record's own band.</param>
        IAugmentItem? Mint(string augmentId, Rarity? rarity = null);

        /// <summary>The copy a file remembers, carried back into the world without a draw. Null when
        /// the catalog no longer declares the record: an augment whose record left the game cannot be
        /// rebuilt, and inventing one would be a different augment.</summary>
        IAugmentItem? Restore(AugmentInstance augment);

        /// <summary>The copy a file remembers, with whatever the file could not tell drawn once, here,
        /// from what the record declares now: the numbers come back untouched, the rarity is drawn for a
        /// file predating rolled rarity, and the laid effect is drawn for one predating effect pools.</summary>
        AugmentInstance? Remembered(string augmentId, IReadOnlyDictionary<string, float> values, Rarity? rarity, string effectId = "");
    }

    /// <param name="augments">What an id means. Asked first on both doors: the record is what says
    /// whether the id is an augment at all and where it stands on the rarity scale.</param>
    /// <param name="minter">The one place a copy's numbers are drawn. Handed the record rather than
    /// the id, so an id nobody declares is answered here instead of reported as a miss — the item
    /// minter offers it every id there is.</param>
    /// <param name="effects">Where the numbers of the effect a copy lays are balanced — the other half
    /// of what its card prints. Lazy and optional for the same reason the ability registry takes it
    /// that way: a composition without an effect registry still mints every augment there is.</param>
    public sealed class AugmentItemMinter(
        IAbilityAugmentCatalog augments,
        AugmentMinter minter,
        Func<IEffectProvider?>? effects = null) : IAugmentItemMinter
    {
        private readonly Func<IEffectProvider?> _effects = effects ?? (static () => null);

        public IAugmentItem? Mint(string augmentId, Rarity? rarity = null) =>
            augments.Find(augmentId) is { } record
                ? new AugmentItem(rarity is { } decided ? minter.Mint(record, decided) : minter.Mint(record), record, _effects())
                : null;

        public IAugmentItem? Restore(AugmentInstance augment) =>
            augments.Find(augment.AugmentId) is { } record ? new AugmentItem(augment, record, _effects()) : null;

        public AugmentInstance? Remembered(string augmentId, IReadOnlyDictionary<string, float> values, Rarity? rarity, string effectId = "") =>
            augments.Find(augmentId) is { } record
                ? new AugmentInstance(
                    augmentId,
                    values,
                    rarity ?? minter.RollRarity(record),
                    // A written effect the record no longer offers is not this record's copy any more:
                    // it is re-drawn rather than carried, the way an unwritten one is.
                    record.EffectPool.ContainsKey(effectId) ? effectId : minter.RollEffect(record))
                : null;
    }
}
