namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Items;
    using Moq;

    /// <summary>
    /// Augment copies for walks that are not about the numbers. Where a slot is filled to ask whether
    /// the fitting rule, the allocation or the save file behaves, the copy carries no numbers of its
    /// own: the fit is judged on the record, and a copy that declares nothing takes every number from
    /// the record it was made of. Walks that ARE about the numbers name them here instead of reaching
    /// for a minter, so what a case is measuring is written in the case.
    /// </summary>
    internal static class AugmentCopies
    {
        internal static AugmentInstance Copy(string augmentId, params (string Property, float Value)[] values) =>
            new(augmentId, values.ToDictionary(rolled => rolled.Property, rolled => rolled.Value), Rarity.Common);

        /// <summary>The seam a file written before copies carried a rarity goes through, standing in for
        /// the record's band with one known answer: what a case then reads off the restored copy is the
        /// draw having happened, and never a value the file could have carried.</summary>
        internal static IAugmentItemMinter LegacyDraw(Rarity drawn)
        {
            var minter = new Mock<IAugmentItemMinter>();
            minter
                .Setup(mock => mock.Remembered(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, float>>(), It.IsAny<Rarity?>(), It.IsAny<string>()))
                .Returns((string id, IReadOnlyDictionary<string, float> values, Rarity? written, string effectId) =>
                    new AugmentInstance(id, values, written ?? drawn, effectId));
            return minter.Object;
        }
    }
}
