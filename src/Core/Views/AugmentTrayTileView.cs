namespace Core.Views
{
    using Enums;
    using Godot;

    /// <summary>
    /// One carried augment as the tray shows it. Read only: the tray is a view of the bag, not a second
    /// place augments live in.
    /// </summary>
    /// <param name="InstanceId">The COPY — what a drag carries and what an install names. Two copies of
    /// one record are different augments, so the record id would not do.</param>
    /// <param name="AugmentId">The record, for art and name.</param>
    /// <param name="DisplayName">The copy's name.</param>
    /// <param name="Description">What this copy does, in the numbers it rolled.</param>
    /// <param name="Icon">The augment's art; null when the record has none yet.</param>
    /// <param name="Rarity">Where the record stands on the item scale — the tile's frame colour.</param>
    /// <param name="Tier">The tier the record is written at, which is the highest slot that takes it.</param>
    public record AugmentTrayTileView(
        string InstanceId,
        string AugmentId,
        string DisplayName,
        string Description,
        Texture2D? Icon,
        Rarity Rarity,
        int Tier);
}
