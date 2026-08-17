namespace Core.Items
{
    using Battle.Abilities;
    using Data.AbilityData;

    /// <summary>
    /// The one door an ornament comes into the world through — a quest reward, a debug line, or the way
    /// back out of an ability. One door because the tier the item carries has to be the tier its record
    /// declares: a second way to build one would be a second answer to what an ornament grants.
    /// </summary>
    public interface IOrnamentMinter
    {
        /// <summary>The ornament that id names, or null when no record declares it. Null rather than a
        /// blank item: an ornament granting nothing is not a lesser ornament, it is a bug wearing the
        /// shape of one.</summary>
        IOrnamentItem? Mint(string ornamentId);
    }

    /// <inheritdoc cref="IOrnamentMinter"/>
    public sealed class OrnamentMinter(IOrnamentCatalog ornaments) : IOrnamentMinter
    {
        public IOrnamentItem? Mint(string ornamentId) =>
            ornaments.Find(ornamentId) is { } record ? Build(record) : null;

        private static IOrnamentItem Build(OrnamentData record) =>
            new OrnamentItem(record.Id, record.Tier, record.Rarity);
    }
}
