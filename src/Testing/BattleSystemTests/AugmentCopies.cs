namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;

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
            new(augmentId, values.ToDictionary(rolled => rolled.Property, rolled => rolled.Value));
    }
}
