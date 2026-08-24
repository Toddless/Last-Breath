namespace Core.Views
{
    using System.Collections.Generic;
    using Enums;
    using Godot;

    /// <summary>
    /// One ability of the socket sheet: the ability on the left, its slots in a row beside it. Pure
    /// display data — no domain object leaves.
    /// </summary>
    /// <param name="AbilityId">Which ability the row is about.</param>
    /// <param name="DisplayName">Its name.</param>
    /// <param name="Description">Its LIVE description, with the augments in its slots already counted
    /// into every number. Empty for a row the player no longer owns the ability of: there is no
    /// instance to read numbers off, only the catalog's name and art.</param>
    /// <param name="Cost">The live cast price, rendered. Empty on an unowned row.</param>
    /// <param name="Cooldown">The live cooldown, rendered. Empty on an unowned row and on a cast that
    /// makes its caster wait for nothing.</param>
    /// <param name="Tags">What the cast counts as — the compatibility axis the augments fit along. Read
    /// off the live instance where there is one, so a tag an augment GRANTS is named; off the catalog's
    /// record otherwise, because an unowned ability still counts as what it is.</param>
    /// <param name="Icon">The ability's art; null when it has none yet.</param>
    /// <param name="Stance">Which stance section the row belongs to.</param>
    /// <param name="IsOwned">Whether the player still holds the ability. False rows exist because their
    /// slots still hold his augments — the row stays until he has taken the last one out, dimmed and
    /// remove-only.</param>
    /// <param name="Cells">The slots, in the order the board gives them: ascending tier, repeats legal,
    /// length whatever the allocation opened. Nothing anywhere assumes three of them.</param>
    public record AbilitySocketRowView(
        string AbilityId,
        string DisplayName,
        string Description,
        string Cost,
        string Cooldown,
        IReadOnlyList<string> Tags,
        Texture2D? Icon,
        Stance Stance,
        bool IsOwned,
        IReadOnlyList<AugmentCellView> Cells);
}
